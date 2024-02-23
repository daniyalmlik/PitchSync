using PitchSync.Shared.Enums;

namespace PitchSync.Shared.DTOs;

public record ParticipantDto(string UserId, string DisplayName, RoomRole Role, DateTime JoinedAt);
public record OnlineUserDto(string UserId, string DisplayName, string? FavoriteTeam);
