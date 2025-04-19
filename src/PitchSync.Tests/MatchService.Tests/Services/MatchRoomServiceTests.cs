using Microsoft.EntityFrameworkCore;
using Moq;
using PitchSync.MatchService.Data;
using PitchSync.MatchService.Entities;
using PitchSync.MatchService.Exceptions;
using PitchSync.MatchService.Services;
using PitchSync.Shared.DTOs;
using PitchSync.Shared.Enums;

namespace MatchService.Tests.Services;

[Trait("Category", "Unit")]
public sealed class MatchRoomServiceTests
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
        mock.Setup(a => a.EnsureParticipantAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    private static CreateMatchRequest PublicRoomRequest() => new(
        Title: "Test Match",
        HomeTeam: "Home FC",
        AwayTeam: "Away FC",
        Competition: null,
        KickoffTime: DateTime.UtcNow.AddHours(1),
        IsPublic: true);

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_AddsRoomAndHostParticipant()
    {
        using var db = CreateDb();
        var sut = new MatchRoomService(db, AllowAllAuth().Object);

        await sut.CreateAsync(PublicRoomRequest(), "user-1", "Alice");

        db.MatchRooms.Should().HaveCount(1);
        db.RoomParticipants.Should().ContainSingle(p =>
            p.UserId == "user-1" && p.Role == RoomRole.Host);
    }

    [Fact]
    public async Task CreateAsync_GeneratesInviteCode_ForPrivateRoom()
    {
        using var db = CreateDb();
        var sut = new MatchRoomService(db, AllowAllAuth().Object);
        var request = new CreateMatchRequest("Title", "Home", "Away", null, DateTime.UtcNow, IsPublic: false);

        var result = await sut.CreateAsync(request, "user-1", "Alice");

        result.InviteCode.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateAsync_NoInviteCode_ForPublicRoom()
    {
        using var db = CreateDb();
        var sut = new MatchRoomService(db, AllowAllAuth().Object);

        var result = await sut.CreateAsync(PublicRoomRequest(), "user-1", "Alice");

        result.InviteCode.Should().BeNull();
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_ForNonParticipantOnPrivateRoom()
    {
        using var db = CreateDb();
        var sut = new MatchRoomService(db, AllowAllAuth().Object);
        var request = new CreateMatchRequest("Title", "Home", "Away", null, DateTime.UtcNow, IsPublic: false);
        await sut.CreateAsync(request, "host", "Host");
        var roomId = db.MatchRooms.Single().Id;

        var result = await sut.GetByIdAsync(roomId, "stranger", default);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsRoom_ForParticipant()
    {
        using var db = CreateDb();
        var sut = new MatchRoomService(db, AllowAllAuth().Object);
        await sut.CreateAsync(PublicRoomRequest(), "host", "Host");
        var roomId = db.MatchRooms.Single().Id;

        var result = await sut.GetByIdAsync(roomId, "host", default);

        result.Should().NotBeNull();
        result!.Id.Should().Be(roomId);
        result.Participants.Should().ContainSingle(p => p.UserId == "host");
    }

    // ── JoinAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task JoinAsync_AddsParticipantAsCommentator()
    {
        using var db = CreateDb();
        var sut = new MatchRoomService(db, AllowAllAuth().Object);
        await sut.CreateAsync(PublicRoomRequest(), "host", "Host");
        var roomId = db.MatchRooms.Single().Id;

        var result = await sut.JoinAsync(roomId, "joiner", "Joiner", null);

        result.Should().NotBeNull();
        result!.Role.Should().Be(RoomRole.Commentator);
        db.RoomParticipants.Should().HaveCount(2);
    }

    [Fact]
    public async Task JoinAsync_WithWrongInviteCode_ReturnsNull()
    {
        using var db = CreateDb();
        var sut = new MatchRoomService(db, AllowAllAuth().Object);
        var request = new CreateMatchRequest("Title", "Home", "Away", null, DateTime.UtcNow, IsPublic: false);
        await sut.CreateAsync(request, "host", "Host");
        var roomId = db.MatchRooms.Single().Id;

        var result = await sut.JoinAsync(roomId, "joiner", "Joiner", "WRONGCODE");

        result.Should().BeNull();
    }

    [Fact]
    public async Task JoinAsync_AlreadyJoined_ReturnsExistingParticipant()
    {
        using var db = CreateDb();
        var sut = new MatchRoomService(db, AllowAllAuth().Object);
        await sut.CreateAsync(PublicRoomRequest(), "host", "Host");
        var roomId = db.MatchRooms.Single().Id;

        // Join once as a new user
        await sut.JoinAsync(roomId, "joiner", "Joiner", null);
        // Join again with same user
        var result = await sut.JoinAsync(roomId, "joiner", "Joiner", null);

        result.Should().NotBeNull();
        db.RoomParticipants.Should().HaveCount(2); // no duplicate
    }

    // ── LeaveAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task LeaveAsync_RemovesParticipant()
    {
        using var db = CreateDb();
        var sut = new MatchRoomService(db, AllowAllAuth().Object);
        await sut.CreateAsync(PublicRoomRequest(), "host", "Host");
        var roomId = db.MatchRooms.Single().Id;
        await sut.JoinAsync(roomId, "joiner", "Joiner", null);

        await sut.LeaveAsync(roomId, "joiner", default);

        db.RoomParticipants.Should().ContainSingle(p => p.UserId == "host");
    }

    [Fact]
    public async Task LeaveAsync_PromotesCommentatorToHost_WhenHostLeaves()
    {
        using var db = CreateDb();
        var sut = new MatchRoomService(db, AllowAllAuth().Object);
        await sut.CreateAsync(PublicRoomRequest(), "host", "Host");
        var roomId = db.MatchRooms.Single().Id;
        await sut.JoinAsync(roomId, "commentator", "Commentator", null);

        await sut.LeaveAsync(roomId, "host", default);

        var promoted = await db.RoomParticipants.SingleAsync(p => p.UserId == "commentator");
        promoted.Role.Should().Be(RoomRole.Host);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_Succeeds_ForHost()
    {
        using var db = CreateDb();
        var sut = new MatchRoomService(db, AllowAllAuth().Object);
        await sut.CreateAsync(PublicRoomRequest(), "host", "Host");
        var roomId = db.MatchRooms.Single().Id;

        var result = await sut.DeleteAsync(roomId, "host", default);

        result.Should().BeTrue();
        db.MatchRooms.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_Throws_ForCommentator()
    {
        using var db = CreateDb();
        var auth = new Mock<IRoomAuthorizationService>();
        auth.Setup(a => a.EnsureHostAsync(It.IsAny<Guid>(), "commentator", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RoomAccessDeniedException("Forbidden"));
        var sut = new MatchRoomService(db, auth.Object);
        // Seed room directly
        var room = new MatchRoom
        {
            Title = "T", HomeTeam = "H", AwayTeam = "A",
            KickoffTime = DateTime.UtcNow, CreatedByUserId = "host"
        };
        db.MatchRooms.Add(room);
        await db.SaveChangesAsync();

        var act = async () => await sut.DeleteAsync(room.Id, "commentator", default);

        await act.Should().ThrowAsync<RoomAccessDeniedException>();
    }

    // ── UpdateStatusAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatusAsync_Succeeds_ForHost()
    {
        using var db = CreateDb();
        var sut = new MatchRoomService(db, AllowAllAuth().Object);
        await sut.CreateAsync(PublicRoomRequest(), "host", "Host");
        var roomId = db.MatchRooms.Single().Id;

        var result = await sut.UpdateStatusAsync(roomId, MatchStatus.Live, "host");

        result.Should().NotBeNull();
        result!.Status.Should().Be(MatchStatus.Live);
    }
}
