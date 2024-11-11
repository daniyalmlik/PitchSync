using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PitchSync.MatchService.Hubs;
using PitchSync.MatchService.Services;
using PitchSync.Shared.DTOs;
using PitchSync.Shared.Enums;
using System.Security.Claims;

namespace PitchSync.MatchService.Controllers;

[ApiController]
[Route("api/matches")]
[Authorize]
public sealed class MatchRoomsController : ControllerBase
{
    private readonly IMatchRoomService _rooms;
    private readonly IHubContext<MatchHub> _hub;
    private readonly IPresenceTracker _presence;

    public MatchRoomsController(IMatchRoomService rooms, IHubContext<MatchHub> hub, IPresenceTracker presence)
    {
        _rooms = rooms;
        _hub = hub;
        _presence = presence;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMatchRequest request, CancellationToken ct)
    {
        var (userId, displayName) = GetUserClaims();
        var room = await _rooms.CreateAsync(request, userId, displayName, ct);
        return CreatedAtAction(nameof(GetById), new { id = room.Id }, room);
    }

    [HttpGet]
    public async Task<IActionResult> ListPublic(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] MatchStatus? status = null,
        CancellationToken ct = default)
    {
        var rooms = await _rooms.ListPublicAsync(page, pageSize, search, status, ct);
        return Ok(rooms);
    }

    [HttpGet("mine")]
    public async Task<IActionResult> ListMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var (userId, _) = GetUserClaims();
        var rooms = await _rooms.ListMyRoomsAsync(userId, page, pageSize, ct);
        return Ok(rooms);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var (userId, _) = GetUserClaims();
        var room = await _rooms.GetByIdAsync(id, userId, ct);
        return room is null ? NotFound() : Ok(room);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var (userId, _) = GetUserClaims();
        var deleted = await _rooms.DeleteAsync(id, userId, ct);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/join")]
    public async Task<IActionResult> Join(Guid id, [FromBody] JoinMatchRequest request, CancellationToken ct)
    {
        var (userId, displayName) = GetUserClaims();
        var participant = await _rooms.JoinAsync(id, userId, displayName, request.InviteCode, ct);

        if (participant is null)
            return BadRequest(new { message = "Room not found or invalid invite code." });

        var group = $"match:{id}";
        await _hub.Clients.Group(group).SendAsync("ParticipantJoined", id.ToString(), participant, ct);
        await _hub.Clients.Group(group).SendAsync("PresenceUpdate", id.ToString(), _presence.GetOnlineUsers(id.ToString()), ct);

        return Ok(participant);
    }

    [HttpPost("{id:guid}/leave")]
    public async Task<IActionResult> Leave(Guid id, CancellationToken ct)
    {
        var (userId, _) = GetUserClaims();
        var left = await _rooms.LeaveAsync(id, userId, ct);

        if (!left)
            return NotFound();

        var group = $"match:{id}";
        await _hub.Clients.Group(group).SendAsync("ParticipantLeft", id.ToString(), userId, ct);
        await _hub.Clients.Group(group).SendAsync("PresenceUpdate", id.ToString(), _presence.GetOnlineUsers(id.ToString()), ct);

        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request, CancellationToken ct)
    {
        var (userId, _) = GetUserClaims();
        var room = await _rooms.UpdateStatusAsync(id, request.Status, userId, ct);
        return room is null ? NotFound() : Ok(room);
    }

    [HttpPatch("{id:guid}/score")]
    public async Task<IActionResult> UpdateScore(Guid id, [FromBody] UpdateScoreRequest request, CancellationToken ct)
    {
        var (userId, _) = GetUserClaims();
        var room = await _rooms.UpdateScoreAsync(id, request.HomeScore, request.AwayScore, userId, ct);
        return room is null ? NotFound() : Ok(room);
    }

    [HttpPatch("{id:guid}/participants/{targetUserId}/role")]
    public async Task<IActionResult> PromoteParticipant(Guid id, string targetUserId, [FromBody] PromoteParticipantRequest request, CancellationToken ct)
    {
        var (userId, _) = GetUserClaims();
        var participant = await _rooms.PromoteParticipantAsync(id, targetUserId, request.Role, userId, ct);
        return participant is null ? NotFound() : Ok(participant);
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

public record PromoteParticipantRequest(RoomRole Role);
