using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PitchSync.IdentityService.Entities;
using PitchSync.IdentityService.Services;
using PitchSync.Shared.Configuration;

namespace IdentityService.Tests.Services;

[Trait("Category", "Unit")]
public sealed class JwtTokenServiceTests
{
    private const string SecretKey = "super-secret-test-key-min-32-chars!!";
    private const string Issuer = "PitchSync";
    private const string Audience = "PitchSync";

    private static JwtTokenService CreateService(int expiryMinutes = 60)
    {
        var config = new JwtConfig { SecretKey = SecretKey, Issuer = Issuer, Audience = Audience, ExpiryMinutes = expiryMinutes };
        var options = Options.Create(config);
        return new JwtTokenService(options);
    }

    private static ApplicationUser MakeUser(string? favoriteTeam = null) => new()
    {
        Id = Guid.NewGuid().ToString(),
        UserName = "alice@example.com",
        Email = "alice@example.com",
        DisplayName = "Alice",
        FavoriteTeam = favoriteTeam,
    };

    [Fact]
    public void GenerateToken_ReturnsNonEmptyToken()
    {
        var sut = CreateService();
        var user = MakeUser();

        var result = sut.GenerateToken(user, []);

        result.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateToken_TokenIsValidJwt()
    {
        var sut = CreateService();
        var user = MakeUser();

        var result = sut.GenerateToken(user, []);

        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));

        var principal = handler.ValidateToken(result.Token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero
        }, out _);

        principal.Should().NotBeNull();
    }

    [Fact]
    public void GenerateToken_ClaimsContainSubEmailDisplayName()
    {
        var sut = CreateService();
        var user = MakeUser();

        var result = sut.GenerateToken(user, []);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
        jwt.Claims.Should().Contain(c => c.Type == "display_name" && c.Value == user.DisplayName);
    }

    [Fact]
    public void GenerateToken_FavoriteTeamClaimIncludedWhenSet()
    {
        var sut = CreateService();
        var user = MakeUser(favoriteTeam: "Real Madrid");

        var result = sut.GenerateToken(user, []);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        jwt.Claims.Should().Contain(c => c.Type == "favorite_team" && c.Value == "Real Madrid");
    }

    [Fact]
    public void GenerateToken_FavoriteTeamClaimAbsentWhenNull()
    {
        var sut = CreateService();
        var user = MakeUser(favoriteTeam: null);

        var result = sut.GenerateToken(user, []);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        jwt.Claims.Should().NotContain(c => c.Type == "favorite_team");
    }

    [Fact]
    public void GenerateToken_RoleClaimsIncluded()
    {
        var sut = CreateService();
        var user = MakeUser();

        var result = sut.GenerateToken(user, ["Admin", "Moderator"]);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Moderator");
    }

    [Fact]
    public void GenerateToken_ExpiresAtMatchesConfig()
    {
        var sut = CreateService(expiryMinutes: 30);
        var user = MakeUser();
        var before = DateTime.UtcNow;

        var result = sut.GenerateToken(user, []);

        result.ExpiresAt.Should().BeCloseTo(before.AddMinutes(30), precision: TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateToken_UserInfoPopulatedCorrectly()
    {
        var sut = CreateService();
        var user = MakeUser(favoriteTeam: "Barcelona");

        var result = sut.GenerateToken(user, []);

        result.User.Id.Should().Be(user.Id);
        result.User.Email.Should().Be(user.Email);
        result.User.DisplayName.Should().Be(user.DisplayName);
        result.User.FavoriteTeam.Should().Be("Barcelona");
    }
}
