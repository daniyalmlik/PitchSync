using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PitchSync.MatchService.Services;
using PitchSync.Shared.DTOs;
using PitchSync.Shared.Enums;
using System.Security.Claims;

namespace PitchSync.MatchService.Hubs;

[Authorize]
public sealed class MatchHub : Hub
{
    private readonly IMatchRoomService _rooms;
    private readonly IMatchEventService _events;
    private readonly IPlayerRatingService _ratings;
    private readonly IRoomAuthorizationService _auth;
    private readonly IPresenceTracker _presence;

    public MatchHub(
        IMatchRoomService rooms,
        IMatchEventService events,
        IPlayerRatingService ratings,
        IRoomAuthorizationService auth,
        IPresenceTracker presence)
    {
        _rooms = rooms;
        _events = events;
        _ratings = ratings;
        _auth = auth;
        _presence = presence;
    }

    private string UserId => Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value;
    private string DisplayName => Context.User!.FindFirst("display_name")?.Value ?? "Unknown";
    private string? FavoriteTeam => Context.User!.FindFirst("favorite_team")?.Value;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    public override async Task OnConnectedAsync()
    {
        var roomIdStr = Context.GetHttpContext()!.Request.Query["roomId"].ToString();

        if (string.IsNullOrEmpty(roomIdStr) || !Guid.TryParse(roomIdStr, out var roomId))
            throw new HubException("Missing or invalid roomId query parameter.");

        var role = await _auth.GetRoleAsync(roomId, UserId);
        if (role is null)
            throw new HubException("Not a participant in this room.");

        var group = Group(roomIdStr);
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
        Context.Items["roomId"] = roomIdStr;

        _presence.AddConnection(roomIdStr, UserId, DisplayName, FavoriteTeam, Context.ConnectionId);

        await Clients.Group(group).SendAsync("PresenceUpdate", roomIdStr, _presence.GetOnlineUsers(roomIdStr));

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var info = _presence.RemoveConnection(Context.ConnectionId);

        if (info.HasValue)
        {
            var (roomId, _) = info.Value;
            await Clients.Group(Group(roomId))
                .SendAsync("PresenceUpdate", roomId, _presence.GetOnlineUsers(roomId));
        }

        await base.OnDisconnectedAsync(exception);
    }

    // ── Hub Methods ──────────────────────────────────────────────────────────

    public async Task PostEvent(PostEventRequest request)
    {
        var roomIdStr = GetRoomId();
        var roomId = Guid.Parse(roomIdStr);

        await _auth.EnsureCommentatorAsync(roomId, UserId);

        var ev = await _events.PostEventAsync(roomId, request, UserId, DisplayName);
        if (ev is null)
            throw new HubException("Room not found.");

        var group = Group(roomIdStr);
        await Clients.Group(group).SendAsync("EventPosted", ev);

        if (request.EventType is MatchEventType.Goal or MatchEventType.OwnGoal)
        {
            var room = await _rooms.GetByIdAsync(roomId, UserId);
            if (room is not null)
                await Clients.Group(group).SendAsync("ScoreUpdated", roomIdStr, room.HomeScore, room.AwayScore);
        }
    }

    public async Task UpdateScore(int homeScore, int awayScore)
    {
        var roomIdStr = GetRoomId();
        var roomId = Guid.Parse(roomIdStr);

        await _auth.EnsureCommentatorAsync(roomId, UserId);

        var room = await _rooms.UpdateScoreAsync(roomId, homeScore, awayScore, UserId);
        if (room is null)
            throw new HubException("Room not found.");

        await Clients.Group(Group(roomIdStr))
            .SendAsync("ScoreUpdated", roomIdStr, room.HomeScore, room.AwayScore);
    }

    public async Task UpdateStatus(MatchStatus status)
    {
        var roomIdStr = GetRoomId();
        var roomId = Guid.Parse(roomIdStr);

        await _auth.EnsureHostAsync(roomId, UserId);

        var room = await _rooms.UpdateStatusAsync(roomId, status, UserId);
        if (room is null)
            throw new HubException("Room not found.");

        await Clients.Group(Group(roomIdStr))
            .SendAsync("StatusChanged", roomIdStr, room.Status);
    }

    public async Task RatePlayer(string playerName, string team, decimal rating)
    {
        var roomIdStr = GetRoomId();
        var roomId = Guid.Parse(roomIdStr);

        await _auth.EnsureCommentatorAsync(roomId, UserId);

        var result = await _ratings.RatePlayerAsync(roomId, playerName, team, rating, UserId);
        if (result is null)
            throw new HubException("Room not found.");

        var allRatings = await _ratings.GetRatingsAsync(roomId, UserId);
        await Clients.Group(Group(roomIdStr))
            .SendAsync("RatingsUpdated", roomIdStr, allRatings);
    }

    public async Task DeleteEvent(Guid eventId)
    {
        var roomIdStr = GetRoomId();
        var roomId = Guid.Parse(roomIdStr);

        await _auth.EnsureHostAsync(roomId, UserId);

        var deleted = await _events.DeleteEventAsync(eventId, UserId);
        if (!deleted)
            throw new HubException("Event not found.");

        var group = Group(roomIdStr);
        await Clients.Group(group).SendAsync("EventDeleted", eventId);

        // Broadcast corrected score (score reverts if the event was a goal)
        var room = await _rooms.GetByIdAsync(roomId, UserId);
        if (room is not null)
            await Clients.Group(group).SendAsync("ScoreUpdated", roomIdStr, room.HomeScore, room.AwayScore);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private string GetRoomId()
        => Context.Items["roomId"] as string
           ?? throw new HubException("Room context not found on this connection.");

    private static string Group(string roomId) => $"match:{roomId}";
}
