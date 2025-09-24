using PitchSync.Shared.DTOs;

namespace PitchSync.MatchService.Services;

public interface IMatchEventService
{
    Task<MatchEventResponse?> PostEventAsync(Guid roomId, PostEventRequest request, string userId, string displayName, CancellationToken ct = default);
    Task<PagedResult<MatchEventResponse>> GetEventsAsync(Guid roomId, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<bool> DeleteEventAsync(Guid eventId, string userId, CancellationToken ct = default);
}
