using Microsoft.EntityFrameworkCore;
using PitchSync.MatchService.Data;
using PitchSync.MatchService.Entities;
using PitchSync.MatchService.Exceptions;
using PitchSync.Shared.Enums;

namespace PitchSync.MatchService.Services;

public sealed class RoomAuthorizationService : IRoomAuthorizationService
{
    private readonly MatchDbContext _db;

    public RoomAuthorizationService(MatchDbContext db)
    {
        _db = db;
    }

    public async Task<RoomParticipant?> GetParticipantAsync(Guid roomId, string userId, CancellationToken ct = default)
        => await _db.RoomParticipants
                    .FirstOrDefaultAsync(p => p.MatchRoomId == roomId && p.UserId == userId, ct);

    public async Task<RoomRole?> GetRoleAsync(Guid roomId, string userId, CancellationToken ct = default)
    {
        var participant = await GetParticipantAsync(roomId, userId, ct);
        return participant?.Role;
    }

    public async Task EnsureParticipantAsync(Guid roomId, string userId, CancellationToken ct = default)
    {
        var participant = await GetParticipantAsync(roomId, userId, ct);
        if (participant is null)
            throw new RoomAccessDeniedException("You are not a participant in this room.");
    }

    public async Task EnsureHostAsync(Guid roomId, string userId, CancellationToken ct = default)
    {
        var participant = await GetParticipantAsync(roomId, userId, ct);

        if (participant?.Role != RoomRole.Host)
            throw new RoomAccessDeniedException("Only the room Host can perform this action.");
    }

    public async Task EnsureCommentatorAsync(Guid roomId, string userId, CancellationToken ct = default)
    {
        var participant = await GetParticipantAsync(roomId, userId, ct);

        if (participant is null || participant.Role == RoomRole.Spectator)
            throw new RoomAccessDeniedException("Host or Commentator role is required for this action.");
    }
}
