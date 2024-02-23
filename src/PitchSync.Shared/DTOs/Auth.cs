namespace PitchSync.Shared.DTOs;

public record RegisterRequest(string Email, string Password, string DisplayName, string? FavoriteTeam = null);
public record LoginRequest(string Email, string Password);
public record TokenResponse(string Token, DateTime ExpiresAt, UserInfo User);
public record UserInfo(string Id, string Email, string DisplayName, string? FavoriteTeam, string? AvatarUrl);
public record UpdateProfileRequest(string? DisplayName, string? FavoriteTeam, string? AvatarUrl);
