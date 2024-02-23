namespace PitchSync.Shared.DTOs;

public record PlayerLineupDto(string PlayerName, int? ShirtNumber, string? Position, bool IsStarting = true);
public record SetLineupRequest(List<PlayerLineupDto> Players);
