using Microsoft.EntityFrameworkCore;
using PitchSync.MatchService.Data;
using PitchSync.MatchService.Entities;
using PitchSync.Shared.DTOs;

namespace PitchSync.MatchService.Services;

public sealed class PlayerLineupService : IPlayerLineupService
{
    private readonly MatchDbContext _db;
    private readonly IRoomAuthorizationService _auth;

    public PlayerLineupService(MatchDbContext db, IRoomAuthorizationService auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task<List<PlayerLineupDto>?> SetLineupAsync(Guid roomId, string team, List<PlayerLineupDto> players, string userId, CancellationToken ct = default)
    {
        await _auth.EnsureCommentatorAsync(roomId, userId, ct);

        var roomExists = await _db.MatchRooms.AnyAsync(r => r.Id == roomId, ct);
        if (!roomExists)
            return null;

        var existing = await _db.PlayerLineups
            .Where(l => l.MatchRoomId == roomId && l.Team == team)
            .ToListAsync(ct);

        _db.PlayerLineups.RemoveRange(existing);

        var newEntries = players.Select(p => new PlayerLineup
        {
            MatchRoomId = roomId,
            Team = team,
            PlayerName = p.PlayerName,
            ShirtNumber = p.ShirtNumber,
            Position = p.Position,
            IsStarting = p.IsStarting,
            AddedByUserId = userId
        }).ToList();

        _db.PlayerLineups.AddRange(newEntries);
        await _db.SaveChangesAsync(ct);

        return newEntries
            .Select(l => new PlayerLineupDto(l.PlayerName, l.ShirtNumber, l.Position, l.IsStarting))
            .ToList();
    }

    public async Task<(List<PlayerLineupDto> Home, List<PlayerLineupDto> Away)?> GetLineupsAsync(Guid roomId, CancellationToken ct = default)
    {
        var roomExists = await _db.MatchRooms.AnyAsync(r => r.Id == roomId, ct);
        if (!roomExists)
            return null;

        var lineups = await _db.PlayerLineups
            .Where(l => l.MatchRoomId == roomId)
            .ToListAsync(ct);

        var home = lineups
            .Where(l => l.Team == "home")
            .Select(l => new PlayerLineupDto(l.PlayerName, l.ShirtNumber, l.Position, l.IsStarting))
            .ToList();

        var away = lineups
            .Where(l => l.Team == "away")
            .Select(l => new PlayerLineupDto(l.PlayerName, l.ShirtNumber, l.Position, l.IsStarting))
            .ToList();

        return (home, away);
    }
}
