namespace PitchSync.Shared.Configuration;

public static class JwtSettings
{
    public const string SectionName = "JwtSettings";
}

public record JwtConfig
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; }
}
