using PitchSync.MatchService.Entities;

namespace PitchSync.MatchService.Services;

public interface IRoomAuthorizationService
{
    Task<RoomParticipant?> GetParticipantAsync(Guid roomId, string userId, CancellationToken ct = default);
    Task EnsureHostAsync(Guid roomId, string userId, CancellationToken ct = default);
    Task EnsureCommentatorAsync(Guid roomId, string userId, CancellationToken ct = default);
}
