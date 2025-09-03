using PitchSync.Shared.Enums;

namespace PitchSync.Shared.DTOs;

public record RoomInviteDto(
    Guid Id,
    Guid MatchRoomId,
    string RoomTitle,
    string HomeTeam,
    string AwayTeam,
    string InvitedByDisplayName,
    InviteStatus Status,
    DateTime CreatedAt);
