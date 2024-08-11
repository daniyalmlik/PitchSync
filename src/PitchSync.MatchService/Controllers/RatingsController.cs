using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PitchSync.MatchService.Services;
using PitchSync.Shared.DTOs;
using System.Security.Claims;

namespace PitchSync.MatchService.Controllers;

[ApiController]
[Route("api/matches/{matchId:guid}/ratings")]
[Authorize]
public sealed class RatingsController : ControllerBase
{
    private readonly IPlayerRatingService _ratings;

    public RatingsController(IPlayerRatingService ratings)
    {
        _ratings = ratings;
    }

    [HttpPut("{team}/{playerName}")]
    public async Task<IActionResult> RatePlayer(Guid matchId, string team, string playerName, [FromBody] RatePlayerRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _ratings.RatePlayerAsync(matchId, playerName, team.ToLower(), request.Rating, userId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetRatings(Guid matchId, CancellationToken ct)
    {
        var userId = GetUserId();
        var ratings = await _ratings.GetRatingsAsync(matchId, userId, ct);
        return Ok(ratings);
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMyRatings(Guid matchId, CancellationToken ct)
    {
        var userId = GetUserId();
        var all = await _ratings.GetRatingsAsync(matchId, userId, ct);
        var mine = all.Where(r => r.MyRating.HasValue).ToList();
        return Ok(mine);
    }

    private string GetUserId()
        => User.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? User.FindFirstValue("sub")
           ?? throw new InvalidOperationException("UserId claim missing.");
}
