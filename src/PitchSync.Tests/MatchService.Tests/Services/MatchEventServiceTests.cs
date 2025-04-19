using Microsoft.EntityFrameworkCore;
using Moq;
using PitchSync.MatchService.Data;
using PitchSync.MatchService.Entities;
using PitchSync.MatchService.Services;
using PitchSync.Shared.DTOs;
using PitchSync.Shared.Enums;

namespace MatchService.Tests.Services;

[Trait("Category", "Unit")]
public sealed class MatchEventServiceTests
{
    private static MatchDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<MatchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static Mock<IRoomAuthorizationService> AllowAllAuth()
    {
        var mock = new Mock<IRoomAuthorizationService>();
        mock.Setup(a => a.EnsureHostAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mock.Setup(a => a.EnsureCommentatorAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    private static async Task<MatchRoom> SeedRoomAsync(MatchDbContext db)
    {
        var room = new MatchRoom
        {
            Title = "Test Match",
            HomeTeam = "Home",
            AwayTeam = "Away",
            KickoffTime = DateTime.UtcNow,
            CreatedByUserId = "host"
        };
        db.MatchRooms.Add(room);
        await db.SaveChangesAsync();
        return room;
    }

    // ── PostEventAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task PostEventAsync_CreatesEvent()
    {
        using var db = CreateDb();
        var room = await SeedRoomAsync(db);
        var sut = new MatchEventService(db, AllowAllAuth().Object);
        var request = new PostEventRequest(Minute: 10, EventType: MatchEventType.Comment);

        var result = await sut.PostEventAsync(room.Id, request, "user-1", "Alice");

        result.Should().NotBeNull();
        db.MatchEvents.Should().ContainSingle(e => e.MatchRoomId == room.Id);
    }

    [Fact]
    public async Task PostEventAsync_Goal_Home_IncrementsHomeScore()
    {
        using var db = CreateDb();
        var room = await SeedRoomAsync(db);
        var sut = new MatchEventService(db, AllowAllAuth().Object);
        var request = new PostEventRequest(Minute: 30, EventType: MatchEventType.Goal, Team: "home");

        await sut.PostEventAsync(room.Id, request, "user-1", "Alice");

        var updated = await db.MatchRooms.FindAsync(room.Id);
        updated!.HomeScore.Should().Be(1);
        updated.AwayScore.Should().Be(0);
    }

    [Fact]
    public async Task PostEventAsync_Goal_Away_IncrementsAwayScore()
    {
        using var db = CreateDb();
        var room = await SeedRoomAsync(db);
        var sut = new MatchEventService(db, AllowAllAuth().Object);
        var request = new PostEventRequest(Minute: 45, EventType: MatchEventType.Goal, Team: "away");

        await sut.PostEventAsync(room.Id, request, "user-1", "Alice");

        var updated = await db.MatchRooms.FindAsync(room.Id);
        updated!.AwayScore.Should().Be(1);
        updated.HomeScore.Should().Be(0);
    }

    [Fact]
    public async Task PostEventAsync_OwnGoal_Home_IncrementsAwayScore()
    {
        using var db = CreateDb();
        var room = await SeedRoomAsync(db);
        var sut = new MatchEventService(db, AllowAllAuth().Object);
        // "home" team scores an OwnGoal → away team gets the point
        var request = new PostEventRequest(Minute: 60, EventType: MatchEventType.OwnGoal, Team: "home");

        await sut.PostEventAsync(room.Id, request, "user-1", "Alice");

        var updated = await db.MatchRooms.FindAsync(room.Id);
        updated!.AwayScore.Should().Be(1);
        updated.HomeScore.Should().Be(0);
    }

    // ── DeleteEventAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteEventAsync_Succeeds_ForHost()
    {
        using var db = CreateDb();
        var room = await SeedRoomAsync(db);
        var sut = new MatchEventService(db, AllowAllAuth().Object);
        var postRequest = new PostEventRequest(Minute: 10, EventType: MatchEventType.Comment);
        var posted = await sut.PostEventAsync(room.Id, postRequest, "user-1", "Alice");

        var deleted = await sut.DeleteEventAsync(posted!.Id, "host", default);

        deleted.Should().BeTrue();
        db.MatchEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteEventAsync_Goal_ReversesScoreIncrement()
    {
        using var db = CreateDb();
        var room = await SeedRoomAsync(db);
        var sut = new MatchEventService(db, AllowAllAuth().Object);
        var goalRequest = new PostEventRequest(Minute: 30, EventType: MatchEventType.Goal, Team: "home");
        var posted = await sut.PostEventAsync(room.Id, goalRequest, "user-1", "Alice");
        // Verify score incremented
        (await db.MatchRooms.FindAsync(room.Id))!.HomeScore.Should().Be(1);

        await sut.DeleteEventAsync(posted!.Id, "host", default);

        var updated = await db.MatchRooms.FindAsync(room.Id);
        updated!.HomeScore.Should().Be(0);
    }

    // ── GetEventsAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetEventsAsync_ReturnsOrderedByMinuteThenCreatedAt()
    {
        using var db = CreateDb();
        var room = await SeedRoomAsync(db);
        var sut = new MatchEventService(db, AllowAllAuth().Object);
        var now = DateTime.UtcNow;

        // Insert events out of order
        db.MatchEvents.AddRange(
            new MatchEvent { MatchRoomId = room.Id, Minute = 45, EventType = MatchEventType.Comment, PostedByUserId = "u", PostedByDisplayName = "U", CreatedAt = now.AddSeconds(2) },
            new MatchEvent { MatchRoomId = room.Id, Minute = 10, EventType = MatchEventType.Comment, PostedByUserId = "u", PostedByDisplayName = "U", CreatedAt = now.AddSeconds(1) },
            new MatchEvent { MatchRoomId = room.Id, Minute = 10, EventType = MatchEventType.Goal,    PostedByUserId = "u", PostedByDisplayName = "U", CreatedAt = now.AddSeconds(3) }
        );
        await db.SaveChangesAsync();

        var events = await sut.GetEventsAsync(room.Id, null, null, default);

        events.Should().HaveCount(3);
        events[0].Minute.Should().Be(10);
        events[1].Minute.Should().Be(10);
        events[1].EventType.Should().Be(MatchEventType.Goal); // later CreatedAt within minute 10
        events[2].Minute.Should().Be(45);
    }
}
