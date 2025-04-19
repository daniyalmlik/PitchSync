using Microsoft.EntityFrameworkCore;
using PitchSync.MatchService.Data;
using PitchSync.MatchService.Entities;
using PitchSync.MatchService.Exceptions;
using PitchSync.MatchService.Services;
using PitchSync.Shared.Enums;

namespace MatchService.Tests.Services;

[Trait("Category", "Unit")]
public sealed class RoomAuthorizationServiceTests
{
    private static MatchDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<MatchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<(MatchRoom Room, RoomAuthorizationService Sut)> SetupAsync(
        MatchDbContext db, string userId, RoomRole role)
    {
        var room = new MatchRoom
        {
            Title = "T", HomeTeam = "H", AwayTeam = "A",
            KickoffTime = DateTime.UtcNow, CreatedByUserId = userId
        };
        db.MatchRooms.Add(room);
        db.RoomParticipants.Add(new RoomParticipant
        {
            MatchRoomId = room.Id,
            UserId = userId,
            DisplayName = "User",
            Role = role
        });
        await db.SaveChangesAsync();

        return (room, new RoomAuthorizationService(db));
    }

    // ── EnsureCommentatorAsync ────────────────────────────────────────────────

    [Fact]
    public async Task EnsureCommentatorAsync_Throws_ForSpectator()
    {
        using var db = CreateDb();
        var (room, sut) = await SetupAsync(db, "spectator", RoomRole.Spectator);

        var act = async () => await sut.EnsureCommentatorAsync(room.Id, "spectator");

        await act.Should().ThrowAsync<RoomAccessDeniedException>();
    }

    [Fact]
    public async Task EnsureCommentatorAsync_Passes_ForCommentator()
    {
        using var db = CreateDb();
        var (room, sut) = await SetupAsync(db, "commentator", RoomRole.Commentator);

        var act = async () => await sut.EnsureCommentatorAsync(room.Id, "commentator");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureCommentatorAsync_Passes_ForHost()
    {
        using var db = CreateDb();
        var (room, sut) = await SetupAsync(db, "host", RoomRole.Host);

        var act = async () => await sut.EnsureCommentatorAsync(room.Id, "host");

        await act.Should().NotThrowAsync();
    }

    // ── EnsureHostAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task EnsureHostAsync_Throws_ForCommentator()
    {
        using var db = CreateDb();
        var (room, sut) = await SetupAsync(db, "commentator", RoomRole.Commentator);

        var act = async () => await sut.EnsureHostAsync(room.Id, "commentator");

        await act.Should().ThrowAsync<RoomAccessDeniedException>();
    }

    [Fact]
    public async Task EnsureHostAsync_Passes_ForHost()
    {
        using var db = CreateDb();
        var (room, sut) = await SetupAsync(db, "host", RoomRole.Host);

        var act = async () => await sut.EnsureHostAsync(room.Id, "host");

        await act.Should().NotThrowAsync();
    }

    // ── EnsureParticipantAsync ────────────────────────────────────────────────

    [Fact]
    public async Task EnsureParticipantAsync_Throws_ForNonParticipant()
    {
        using var db = CreateDb();
        var (room, sut) = await SetupAsync(db, "host", RoomRole.Host);

        var act = async () => await sut.EnsureParticipantAsync(room.Id, "stranger");

        await act.Should().ThrowAsync<RoomAccessDeniedException>();
    }
}
