using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PitchSync.MatchService.Data;
using PitchSync.MatchService.Filters;
using PitchSync.MatchService.Hubs;
using PitchSync.MatchService.Services;
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
builder.Services.AddDbContext<MatchDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(maxRetryCount: 5)));

// ── CORS ─────────────────────────────────────────────────────────────────────
var corsOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:4200", "http://localhost:5000"];

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy
            .WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

// ── Authentication / JWT (consumer only — never issues tokens) ────────────────
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

    // SignalR passes the JWT as a query string param rather than a header
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                context.Token = accessToken;

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// ── Application Services ─────────────────────────────────────────────────────
builder.Services.AddSingleton<IPresenceTracker, PresenceTracker>();
builder.Services.AddScoped<IRoomAuthorizationService, RoomAuthorizationService>();
builder.Services.AddScoped<IMatchRoomService, MatchRoomService>();
builder.Services.AddScoped<IMatchEventService, MatchEventService>();
builder.Services.AddScoped<IPlayerLineupService, PlayerLineupService>();
builder.Services.AddScoped<IPlayerRatingService, PlayerRatingService>();

// ── Controllers + Global Exception Filter ────────────────────────────────────
builder.Services.AddControllers(options =>
    options.Filters.Add<RoomAccessExceptionFilter>());

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR().AddMessagePackProtocol();

// ── Health Checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "match-db");

// ── Pipeline ─────────────────────────────────────────────────────────────────
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MatchDbContext>();
    db.Database.Migrate();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<MatchHub>("/hubs/match");
app.MapHealthChecks("/healthz");

app.Run();

public partial class Program { }
