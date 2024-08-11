using PitchSync.Shared.DTOs;

namespace PitchSync.MatchService.Services;

public interface IPlayerLineupService
{
    Task<List<PlayerLineupDto>?> SetLineupAsync(Guid roomId, string team, List<PlayerLineupDto> players, string userId, CancellationToken ct = default);
    Task<(List<PlayerLineupDto> Home, List<PlayerLineupDto> Away)?> GetLineupsAsync(Guid roomId, CancellationToken ct = default);
}
