using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PitchSync.MatchService.Services;
using PitchSync.Shared.DTOs;
using System.Security.Claims;

namespace PitchSync.MatchService.Controllers;

[ApiController]
[Route("api/matches/{matchId:guid}/events")]
[Authorize]
public sealed class EventsController : ControllerBase
{
    private readonly IMatchEventService _events;

    public EventsController(IMatchEventService events)
    {
        _events = events;
    }

    [HttpPost]
    public async Task<IActionResult> PostEvent(Guid matchId, [FromBody] PostEventRequest request, CancellationToken ct)
    {
        var (userId, displayName) = GetUserClaims();
        var ev = await _events.PostEventAsync(matchId, request, userId, displayName, ct);
        return ev is null ? NotFound() : Ok(ev);
    }

    [HttpGet]
    public async Task<IActionResult> GetEvents(
        Guid matchId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var events = await _events.GetEventsAsync(matchId, page, pageSize, ct);
        return Ok(events);
    }

    [HttpDelete("{eventId:guid}")]
    public async Task<IActionResult> DeleteEvent(Guid matchId, Guid eventId, CancellationToken ct)
    {
        var (userId, _) = GetUserClaims();
        var deleted = await _events.DeleteEventAsync(eventId, userId, ct);
        return deleted ? NoContent() : NotFound();
    }

    private (string userId, string displayName) GetUserClaims()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new InvalidOperationException("UserId claim missing.");
        var displayName = User.FindFirstValue("display_name") ?? "Unknown";
        return (userId, displayName);
    }
}
