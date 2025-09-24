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
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var rooms = await _rooms.ListPublicAsync(page, pageSize, search, status, ct);
        return Ok(rooms);
    }

    [HttpGet("mine")]
    public async Task<IActionResult> ListMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);
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

    [HttpPost("{id:guid}/invite")]
    public async Task<IActionResult> InviteParticipant(Guid id, [FromBody] InviteParticipantRequest request, CancellationToken ct)
    {
        var (userId, _) = GetUserClaims();
        var invite = await _rooms.InviteParticipantAsync(id, request.UserId, request.DisplayName, userId, ct);

        if (invite is null)
            return Conflict(new { message = "User is already a participant in this room." });

        await _hub.Clients.Group($"user:{request.UserId}").SendAsync("InviteReceived", invite, ct);

        return Ok(invite);
    }

    [HttpGet("invites")]
    public async Task<IActionResult> GetMyInvites(CancellationToken ct)
    {
        var (userId, _) = GetUserClaims();
        var invites = await _rooms.GetPendingInvitesAsync(userId, ct);
        return Ok(invites);
    }

    [HttpPost("invites/{inviteId:guid}/accept")]
    public async Task<IActionResult> AcceptInvite(Guid inviteId, CancellationToken ct)
    {
        var (userId, _) = GetUserClaims();
        var result = await _rooms.AcceptInviteAsync(inviteId, userId, ct);

        if (result is null)
            return NotFound();

        var (matchRoomId, participant) = result.Value;
        var group = $"match:{matchRoomId}";
        await _hub.Clients.Group(group).SendAsync("ParticipantJoined", matchRoomId.ToString(), participant, ct);

        return Ok(new { matchRoomId, participant });
    }

    [HttpPost("invites/{inviteId:guid}/decline")]
    public async Task<IActionResult> DeclineInvite(Guid inviteId, CancellationToken ct)
    {
        var (userId, _) = GetUserClaims();
        var ok = await _rooms.DeclineInviteAsync(inviteId, userId, ct);
        return ok ? NoContent() : NotFound();
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
