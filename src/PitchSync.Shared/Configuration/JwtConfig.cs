namespace PitchSync.Shared.Configuration;

public static class JwtSettings
{
    public const string SectionName = "JwtSettings";
}

public record JwtConfig(string SecretKey, string Issuer, string Audience, int ExpiryMinutes);
