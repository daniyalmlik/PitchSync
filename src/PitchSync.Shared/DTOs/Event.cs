using PitchSync.Shared.Enums;

namespace PitchSync.Shared.DTOs;

public record PostEventRequest(
    int Minute,
    MatchEventType EventType,
    string? Team,
    string? PlayerName,
    string? SecondaryPlayerName,
    string? Description);

public record MatchEventResponse(
    Guid Id,
    int Minute,
    MatchEventType EventType,
    string? Team,
    string? PlayerName,
    string? SecondaryPlayerName,
    string? Description,
    string PostedByUserId,
    string PostedByDisplayName,
    DateTime CreatedAt);
