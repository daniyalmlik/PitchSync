using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PitchSync.MatchService.Services;
using PitchSync.Shared.DTOs;
using System.Security.Claims;

namespace PitchSync.MatchService.Controllers;

[ApiController]
[Route("api/matches/{matchId:guid}/lineups")]
[Authorize]
public sealed class LineupsController : ControllerBase
{
    private readonly IPlayerLineupService _lineups;

    public LineupsController(IPlayerLineupService lineups)
    {
        _lineups = lineups;
    }

    [HttpPut("{team}")]
    public async Task<IActionResult> SetLineup(Guid matchId, string team, [FromBody] SetLineupRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _lineups.SetLineupAsync(matchId, team.ToLower(), request.Players, userId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetLineups(Guid matchId, CancellationToken ct)
    {
        var result = await _lineups.GetLineupsAsync(matchId, ct);
        if (result is null)
            return NotFound();

        return Ok(new { HomeLineup = result.Value.Home, AwayLineup = result.Value.Away });
    }

    private string GetUserId()
        => User.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? User.FindFirstValue("sub")
           ?? throw new InvalidOperationException("UserId claim missing.");
}
