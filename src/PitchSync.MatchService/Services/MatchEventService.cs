using Microsoft.EntityFrameworkCore;
using PitchSync.MatchService.Data;
using PitchSync.MatchService.Entities;
using PitchSync.Shared.DTOs;
using PitchSync.Shared.Enums;

namespace PitchSync.MatchService.Services;

public sealed class MatchEventService : IMatchEventService
{
    private readonly MatchDbContext _db;
    private readonly IRoomAuthorizationService _auth;

    public MatchEventService(MatchDbContext db, IRoomAuthorizationService auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task<MatchEventResponse?> PostEventAsync(Guid roomId, PostEventRequest request, string userId, string displayName, CancellationToken ct = default)
    {
        await _auth.EnsureCommentatorAsync(roomId, userId, ct);

        var room = await _db.MatchRooms.FindAsync([roomId], ct);
        if (room is null)
            return null;

        var ev = new MatchEvent
        {
            MatchRoomId = roomId,
            PostedByUserId = userId,
            PostedByDisplayName = displayName,
            Minute = request.Minute,
            EventType = request.EventType,
            Team = request.Team,
            PlayerName = request.PlayerName,
            SecondaryPlayerName = request.SecondaryPlayerName,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow
        };

        _db.MatchEvents.Add(ev);

        // Auto-update score on goal events
        if (request.EventType == MatchEventType.Goal)
        {
            if (request.Team == "home") room.HomeScore++;
            else if (request.Team == "away") room.AwayScore++;
        }
        else if (request.EventType == MatchEventType.OwnGoal)
        {
            // Own goal scores for the opposing team
            if (request.Team == "home") room.AwayScore++;
            else if (request.Team == "away") room.HomeScore++;
        }

        await _db.SaveChangesAsync(ct);

        return MapToResponse(ev);
    }

    public async Task<List<MatchEventResponse>> GetEventsAsync(Guid roomId, int? page, int? pageSize, CancellationToken ct = default)
    {
        var query = _db.MatchEvents
            .Where(e => e.MatchRoomId == roomId)
            .OrderBy(e => e.Minute)
            .ThenBy(e => e.CreatedAt);

        if (page.HasValue && pageSize.HasValue)
        {
            return await query
                .Skip((page.Value - 1) * pageSize.Value)
                .Take(pageSize.Value)
                .Select(e => MapToResponse(e))
                .ToListAsync(ct);
        }

        return await query
            .Select(e => MapToResponse(e))
            .ToListAsync(ct);
    }

    public async Task<bool> DeleteEventAsync(Guid eventId, string userId, CancellationToken ct = default)
    {
        var ev = await _db.MatchEvents
            .Include(e => e.MatchRoom)
            .FirstOrDefaultAsync(e => e.Id == eventId, ct);

        if (ev is null)
            return false;

        await _auth.EnsureHostAsync(ev.MatchRoomId, userId, ct);

        // Revert score if this was a goal event
        if (ev.EventType == MatchEventType.Goal)
        {
            if (ev.Team == "home") ev.MatchRoom.HomeScore = Math.Max(0, ev.MatchRoom.HomeScore - 1);
            else if (ev.Team == "away") ev.MatchRoom.AwayScore = Math.Max(0, ev.MatchRoom.AwayScore - 1);
        }
        else if (ev.EventType == MatchEventType.OwnGoal)
        {
            if (ev.Team == "home") ev.MatchRoom.AwayScore = Math.Max(0, ev.MatchRoom.AwayScore - 1);
            else if (ev.Team == "away") ev.MatchRoom.HomeScore = Math.Max(0, ev.MatchRoom.HomeScore - 1);
        }

        _db.MatchEvents.Remove(ev);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static MatchEventResponse MapToResponse(MatchEvent e) => new(
        e.Id, e.Minute, e.EventType, e.Team, e.PlayerName,
        e.SecondaryPlayerName, e.Description, e.PostedByUserId,
        e.PostedByDisplayName, e.CreatedAt);
}
