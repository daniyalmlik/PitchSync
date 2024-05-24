using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PitchSync.IdentityService.Data;
using PitchSync.IdentityService.Entities;
using PitchSync.IdentityService.Services;
using PitchSync.Shared.Configuration;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ────────────────────────────────────────────────────────────
builder.Services.Configure<JwtConfig>(
    builder.Configuration.GetSection(JwtSettings.SectionName));

var jwtConfig = builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtConfig>()
    ?? throw new InvalidOperationException("JwtSettings section is missing from configuration.");

// ── Database ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(maxRetryCount: 5)));

// ── Identity ─────────────────────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<IdentityDbContext>()
.AddDefaultTokenProviders();

// ── Authentication / JWT ─────────────────────────────────────────────────────
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtConfig.Issuer,
        ValidAudience = jwtConfig.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.SecretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ── Application Services ─────────────────────────────────────────────────────
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// ── Health Checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "identity-db");

builder.Services.AddControllers();

// ── Pipeline ─────────────────────────────────────────────────────────────────
var app = builder.Build();

// Auto-migrate in Development so docker-compose up just works
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    db.Database.Migrate();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/healthz");

app.Run();

// Make Program accessible to WebApplicationFactory in integration tests
public partial class Program { }
