using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PitchSync.IdentityService.Entities;
using PitchSync.Shared.Configuration;
using PitchSync.Shared.DTOs;

namespace PitchSync.IdentityService.Services;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtConfig _config;

    public JwtTokenService(IOptions<JwtConfig> config)
    {
        _config = config.Value;
    }

    public TokenResponse GenerateToken(ApplicationUser user, IList<string> roles)
    {
        var claims = BuildClaims(user, roles);
        var expiresAt = DateTime.UtcNow.AddMinutes(_config.ExpiryMinutes);
        var token = CreateToken(claims, expiresAt);

        return new TokenResponse(
            Token: new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt: expiresAt,
            User: new UserInfo(
                Id: user.Id,
                Email: user.Email!,
                DisplayName: user.DisplayName,
                FavoriteTeam: user.FavoriteTeam,
                AvatarUrl: user.AvatarUrl));
    }

    private List<Claim> BuildClaims(ApplicationUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new("display_name", user.DisplayName),
        };

        if (user.FavoriteTeam is not null)
            claims.Add(new Claim("favorite_team", user.FavoriteTeam));

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        return claims;
    }

    private JwtSecurityToken CreateToken(IEnumerable<Claim> claims, DateTime expiresAt)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        return new JwtSecurityToken(
            issuer: _config.Issuer,
            audience: _config.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);
    }
}
