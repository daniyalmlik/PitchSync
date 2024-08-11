using Microsoft.EntityFrameworkCore;
using PitchSync.MatchService.Data;
using PitchSync.MatchService.Entities;
using PitchSync.Shared.DTOs;
using PitchSync.Shared.Enums;
using System.Security.Cryptography;
using System.Text;

namespace PitchSync.MatchService.Services;

public sealed class MatchRoomService : IMatchRoomService
{
    private readonly MatchDbContext _db;
    private readonly IRoomAuthorizationService _auth;

    public MatchRoomService(MatchDbContext db, IRoomAuthorizationService auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task<MatchRoomResponse> CreateAsync(CreateMatchRequest request, string userId, string displayName, CancellationToken ct = default)
    {
        var room = new MatchRoom
        {
            Title = request.Title,
            HomeTeam = request.HomeTeam,
            AwayTeam = request.AwayTeam,
            Competition = request.Competition,
            KickoffTime = request.KickoffTime,
            IsPublic = request.IsPublic,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            InviteCode = request.IsPublic ? null : GenerateInviteCode()
        };

        var host = new RoomParticipant
        {
            MatchRoomId = room.Id,
            UserId = userId,
            DisplayName = displayName,
            Role = RoomRole.Host,
            JoinedAt = DateTime.UtcNow
        };

        _db.MatchRooms.Add(room);
        _db.RoomParticipants.Add(host);
        await _db.SaveChangesAsync(ct);

        return MapToResponse(room, [host], [], [], []);
    }

    public async Task<MatchRoomResponse?> GetByIdAsync(Guid roomId, string userId, CancellationToken ct = default)
    {
        var room = await _db.MatchRooms
            .Include(r => r.Participants)
            .Include(r => r.Events.OrderBy(e => e.Minute).ThenBy(e => e.CreatedAt))
            .Include(r => r.PlayerLineups)
            .Include(r => r.PlayerRatings)
            .FirstOrDefaultAsync(r => r.Id == roomId, ct);

        if (room is null)
            return null;

        if (!room.IsPublic)
        {
            var isParticipant = room.Participants.Any(p => p.UserId == userId);
            if (!isParticipant)
                return null;
        }

        return MapToResponse(
            room,
            room.Participants.ToList(),
            room.Events.ToList(),
            room.PlayerLineups.ToList(),
            room.PlayerRatings.ToList(),
            userId);
    }

    public async Task<List<MatchRoomSummary>> ListPublicAsync(int page, int pageSize, string? search, MatchStatus? status, CancellationToken ct = default)
    {
        var query = _db.MatchRooms
            .Where(r => r.IsPublic)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = search.ToLower();
            query = query.Where(r =>
                r.Title.ToLower().Contains(lower) ||
                r.HomeTeam.ToLower().Contains(lower) ||
                r.AwayTeam.ToLower().Contains(lower));
        }

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        return await query
            .OrderByDescending(r => r.KickoffTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new MatchRoomSummary(
                r.Id,
                r.Title,
                r.HomeTeam,
                r.AwayTeam,
                r.Status,
                r.HomeScore,
                r.AwayScore,
                r.KickoffTime,
                r.Participants.Count))
            .ToListAsync(ct);
    }

    public async Task<List<MatchRoomSummary>> ListMyRoomsAsync(string userId, int page, int pageSize, CancellationToken ct = default)
    {
        return await _db.MatchRooms
            .Where(r => r.Participants.Any(p => p.UserId == userId))
            .OrderByDescending(r => r.KickoffTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new MatchRoomSummary(
                r.Id,
                r.Title,
                r.HomeTeam,
                r.AwayTeam,
                r.Status,
                r.HomeScore,
                r.AwayScore,
                r.KickoffTime,
                r.Participants.Count))
            .ToListAsync(ct);
    }

    public async Task<ParticipantDto?> JoinAsync(Guid roomId, string userId, string displayName, string? inviteCode, CancellationToken ct = default)
    {
        var room = await _db.MatchRooms
            .Include(r => r.Participants)
            .FirstOrDefaultAsync(r => r.Id == roomId, ct);

        if (room is null)
            return null;

        var existing = room.Participants.FirstOrDefault(p => p.UserId == userId);
        if (existing is not null)
            return new ParticipantDto(existing.UserId, existing.DisplayName, existing.Role, existing.JoinedAt);

        if (!room.IsPublic && room.InviteCode != inviteCode)
            return null;

        var participant = new RoomParticipant
        {
            MatchRoomId = roomId,
            UserId = userId,
            DisplayName = displayName,
            Role = RoomRole.Commentator,
            JoinedAt = DateTime.UtcNow
        };

        _db.RoomParticipants.Add(participant);
        await _db.SaveChangesAsync(ct);

        return new ParticipantDto(participant.UserId, participant.DisplayName, participant.Role, participant.JoinedAt);
    }

    public async Task<bool> LeaveAsync(Guid roomId, string userId, CancellationToken ct = default)
    {
        var room = await _db.MatchRooms
            .Include(r => r.Participants)
            .FirstOrDefaultAsync(r => r.Id == roomId, ct);

        if (room is null)
            return false;

        var participant = room.Participants.FirstOrDefault(p => p.UserId == userId);
        if (participant is null)
            return false;

        _db.RoomParticipants.Remove(participant);

        var remaining = room.Participants.Where(p => p.UserId != userId).ToList();

        if (remaining.Count == 0)
        {
            _db.MatchRooms.Remove(room);
        }
        else if (participant.Role == RoomRole.Host)
        {
            var newHost = remaining
                .Where(p => p.Role == RoomRole.Commentator)
                .OrderBy(p => p.JoinedAt)
                .FirstOrDefault()
                ?? remaining.OrderBy(p => p.JoinedAt).First();

            newHost.Role = RoomRole.Host;
        }

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<MatchRoomResponse?> UpdateStatusAsync(Guid roomId, MatchStatus status, string userId, CancellationToken ct = default)
    {
        await _auth.EnsureHostAsync(roomId, userId, ct);

        var room = await LoadFullRoomAsync(roomId, ct);
        if (room is null)
            return null;

        room.Status = status;
        await _db.SaveChangesAsync(ct);

        return MapToResponse(room, room.Participants.ToList(), room.Events.ToList(), room.PlayerLineups.ToList(), room.PlayerRatings.ToList());
    }

    public async Task<MatchRoomResponse?> UpdateScoreAsync(Guid roomId, int homeScore, int awayScore, string userId, CancellationToken ct = default)
    {
        await _auth.EnsureCommentatorAsync(roomId, userId, ct);

        var room = await LoadFullRoomAsync(roomId, ct);
        if (room is null)
            return null;

        room.HomeScore = homeScore;
        room.AwayScore = awayScore;
        await _db.SaveChangesAsync(ct);

        return MapToResponse(room, room.Participants.ToList(), room.Events.ToList(), room.PlayerLineups.ToList(), room.PlayerRatings.ToList());
    }

    public async Task<bool> DeleteAsync(Guid roomId, string userId, CancellationToken ct = default)
    {
        await _auth.EnsureHostAsync(roomId, userId, ct);

        var room = await _db.MatchRooms.FindAsync([roomId], ct);
        if (room is null)
            return false;

        _db.MatchRooms.Remove(room);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ParticipantDto?> PromoteParticipantAsync(Guid roomId, string targetUserId, RoomRole newRole, string requestingUserId, CancellationToken ct = default)
    {
        await _auth.EnsureHostAsync(roomId, requestingUserId, ct);

        var participant = await _db.RoomParticipants
            .FirstOrDefaultAsync(p => p.MatchRoomId == roomId && p.UserId == targetUserId, ct);

        if (participant is null)
            return null;

        participant.Role = newRole;
        await _db.SaveChangesAsync(ct);

        return new ParticipantDto(participant.UserId, participant.DisplayName, participant.Role, participant.JoinedAt);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<MatchRoom?> LoadFullRoomAsync(Guid roomId, CancellationToken ct)
        => await _db.MatchRooms
                    .Include(r => r.Participants)
                    .Include(r => r.Events.OrderBy(e => e.Minute).ThenBy(e => e.CreatedAt))
                    .Include(r => r.PlayerLineups)
                    .Include(r => r.PlayerRatings)
                    .FirstOrDefaultAsync(r => r.Id == roomId, ct);

    private static MatchRoomResponse MapToResponse(
        MatchRoom room,
        List<RoomParticipant> participants,
        List<MatchEvent> events,
        List<PlayerLineup> lineups,
        List<PlayerRating> ratings,
        string? currentUserId = null)
    {
        var homeLineup = lineups
            .Where(l => l.Team == "home")
            .Select(l => new PlayerLineupDto(l.PlayerName, l.ShirtNumber, l.Position, l.IsStarting))
            .ToList();

        var awayLineup = lineups
            .Where(l => l.Team == "away")
            .Select(l => new PlayerLineupDto(l.PlayerName, l.ShirtNumber, l.Position, l.IsStarting))
            .ToList();

        return new MatchRoomResponse(
            Id: room.Id,
            Title: room.Title,
            HomeTeam: room.HomeTeam,
            AwayTeam: room.AwayTeam,
            Competition: room.Competition,
            KickoffTime: room.KickoffTime,
            Status: room.Status,
            HomeScore: room.HomeScore,
            AwayScore: room.AwayScore,
            IsPublic: room.IsPublic,
            InviteCode: room.InviteCode,
            CreatedByUserId: room.CreatedByUserId,
            CreatedAt: room.CreatedAt,
            Participants: participants
                .Select(p => new ParticipantDto(p.UserId, p.DisplayName, p.Role, p.JoinedAt))
                .ToList(),
            Events: events
                .Select(e => new MatchEventResponse(
                    e.Id, e.Minute, e.EventType, e.Team, e.PlayerName,
                    e.SecondaryPlayerName, e.Description, e.PostedByUserId,
                    e.PostedByDisplayName, e.CreatedAt))
                .ToList(),
            HomeLineup: homeLineup,
            AwayLineup: awayLineup);
    }

    private static string GenerateInviteCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var bytes = RandomNumberGenerator.GetBytes(8);
        var sb = new StringBuilder(8);
        foreach (var b in bytes)
            sb.Append(chars[b % chars.Length]);
        return sb.ToString();
    }
}
