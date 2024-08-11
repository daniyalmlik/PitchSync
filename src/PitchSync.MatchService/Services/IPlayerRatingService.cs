using PitchSync.Shared.DTOs;

namespace PitchSync.MatchService.Services;

public interface IPlayerRatingService
{
    Task<PlayerRatingResponse?> RatePlayerAsync(Guid roomId, string playerName, string team, decimal rating, string userId, CancellationToken ct = default);
    Task<List<PlayerRatingResponse>> GetRatingsAsync(Guid roomId, string? userId, CancellationToken ct = default);
}
