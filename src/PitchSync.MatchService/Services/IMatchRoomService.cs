using PitchSync.Shared.DTOs;
using PitchSync.Shared.Enums;

namespace PitchSync.MatchService.Services;

public interface IMatchRoomService
{
    Task<MatchRoomResponse> CreateAsync(CreateMatchRequest request, string userId, string displayName, CancellationToken ct = default);
    Task<MatchRoomResponse?> GetByIdAsync(Guid roomId, string userId, CancellationToken ct = default);
    Task<List<MatchRoomSummary>> ListPublicAsync(int page, int pageSize, string? search, MatchStatus? status, CancellationToken ct = default);
    Task<List<MatchRoomSummary>> ListMyRoomsAsync(string userId, int page, int pageSize, CancellationToken ct = default);
    Task<ParticipantDto?> JoinAsync(Guid roomId, string userId, string displayName, string? inviteCode, CancellationToken ct = default);
    Task<bool> LeaveAsync(Guid roomId, string userId, CancellationToken ct = default);
    Task<MatchRoomResponse?> UpdateStatusAsync(Guid roomId, MatchStatus status, string userId, CancellationToken ct = default);
    Task<MatchRoomResponse?> UpdateScoreAsync(Guid roomId, int homeScore, int awayScore, string userId, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid roomId, string userId, CancellationToken ct = default);
    Task<ParticipantDto?> PromoteParticipantAsync(Guid roomId, string targetUserId, RoomRole newRole, string requestingUserId, CancellationToken ct = default);
    Task<RoomInviteDto?> InviteParticipantAsync(Guid roomId, string targetUserId, string displayName, string hostUserId, CancellationToken ct = default);
    Task<IReadOnlyList<RoomInviteDto>> GetPendingInvitesAsync(string userId, CancellationToken ct = default);
    Task<(Guid MatchRoomId, ParticipantDto Participant)?> AcceptInviteAsync(Guid inviteId, string userId, CancellationToken ct = default);
    Task<bool> DeclineInviteAsync(Guid inviteId, string userId, CancellationToken ct = default);
}
