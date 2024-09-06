using Microsoft.EntityFrameworkCore;
using PitchSync.MatchService.Data;
using PitchSync.MatchService.Entities;
using PitchSync.Shared.DTOs;

namespace PitchSync.MatchService.Services;

public sealed class PlayerRatingService : IPlayerRatingService
{
    private readonly MatchDbContext _db;
    private readonly IRoomAuthorizationService _auth;

    public PlayerRatingService(MatchDbContext db, IRoomAuthorizationService auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task<PlayerRatingResponse?> RatePlayerAsync(Guid roomId, string playerName, string team, decimal rating, string userId, CancellationToken ct = default)
    {
        await _auth.EnsureCommentatorAsync(roomId, userId, ct);

        var roomExists = await _db.MatchRooms.AnyAsync(r => r.Id == roomId, ct);
        if (!roomExists)
            return null;

        // Clamp and round rating to 1 decimal place
        var clamped = Math.Clamp(rating, 1.0m, 10.0m);
        var rounded = Math.Round(clamped, 1);

        var existing = await _db.PlayerRatings
            .FirstOrDefaultAsync(r =>
                r.MatchRoomId == roomId &&
                r.PlayerName == playerName &&
                r.Team == team &&
                r.UserId == userId, ct);

        if (existing is not null)
        {
            existing.Rating = rounded;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var now = DateTime.UtcNow;
            _db.PlayerRatings.Add(new PlayerRating
            {
                MatchRoomId = roomId,
                PlayerName = playerName,
                Team = team,
                UserId = userId,
                Rating = rounded,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await _db.SaveChangesAsync(ct);

        return await BuildRatingResponseAsync(roomId, playerName, team, userId, ct);
    }

    public async Task<List<PlayerRatingResponse>> GetRatingsAsync(Guid roomId, string? userId, CancellationToken ct = default)
    {
        var ratings = await _db.PlayerRatings
            .Where(r => r.MatchRoomId == roomId)
            .ToListAsync(ct);

        return ratings
            .GroupBy(r => new { r.PlayerName, r.Team })
            .Select(g => new PlayerRatingResponse(
                PlayerName: g.Key.PlayerName,
                Team: g.Key.Team,
                AverageRating: Math.Round(g.Average(r => r.Rating), 1),
                RatingCount: g.Count(),
                MyRating: userId is not null
                    ? g.FirstOrDefault(r => r.UserId == userId)?.Rating
                    : null))
            .ToList();
    }

    private async Task<PlayerRatingResponse> BuildRatingResponseAsync(Guid roomId, string playerName, string team, string userId, CancellationToken ct)
    {
        var allRatings = await _db.PlayerRatings
            .Where(r => r.MatchRoomId == roomId && r.PlayerName == playerName && r.Team == team)
            .ToListAsync(ct);

        var average = Math.Round(allRatings.Average(r => r.Rating), 1);
        var myRating = allRatings.FirstOrDefault(r => r.UserId == userId)?.Rating;

        return new PlayerRatingResponse(playerName, team, average, allRatings.Count, myRating);
    }
}
