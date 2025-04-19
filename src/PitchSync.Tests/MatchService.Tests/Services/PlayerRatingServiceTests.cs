using Microsoft.EntityFrameworkCore;
using Moq;
using PitchSync.MatchService.Data;
using PitchSync.MatchService.Entities;
using PitchSync.MatchService.Services;

namespace MatchService.Tests.Services;

[Trait("Category", "Unit")]
public sealed class PlayerRatingServiceTests
{
    private static MatchDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<MatchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static Mock<IRoomAuthorizationService> AllowAllAuth()
    {
        var mock = new Mock<IRoomAuthorizationService>();
        mock.Setup(a => a.EnsureCommentatorAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    private static async Task<MatchRoom> SeedRoomAsync(MatchDbContext db)
    {
        var room = new MatchRoom
        {
            Title = "Test Match", HomeTeam = "Home", AwayTeam = "Away",
            KickoffTime = DateTime.UtcNow, CreatedByUserId = "host"
        };
        db.MatchRooms.Add(room);
        await db.SaveChangesAsync();
        return room;
    }

    [Fact]
    public async Task RatePlayerAsync_CreatesNewRating()
    {
        using var db = CreateDb();
        var room = await SeedRoomAsync(db);
        var sut = new PlayerRatingService(db, AllowAllAuth().Object);

        var result = await sut.RatePlayerAsync(room.Id, "Ronaldo", "home", 9.5m, "user-1");

        result.Should().NotBeNull();
        db.PlayerRatings.Should().ContainSingle(r => r.PlayerName == "Ronaldo" && r.UserId == "user-1");
    }

    [Fact]
    public async Task RatePlayerAsync_UpdatesExistingRating_Upsert()
    {
        using var db = CreateDb();
        var room = await SeedRoomAsync(db);
        var sut = new PlayerRatingService(db, AllowAllAuth().Object);

        await sut.RatePlayerAsync(room.Id, "Messi", "home", 8.0m, "user-1");
        await sut.RatePlayerAsync(room.Id, "Messi", "home", 9.0m, "user-1");

        db.PlayerRatings.Should().ContainSingle(r => r.PlayerName == "Messi" && r.UserId == "user-1");
        db.PlayerRatings.Single().Rating.Should().Be(9.0m);
    }

    [Fact]
    public async Task RatePlayerAsync_ClampsToMin_WhenBelow1()
    {
        using var db = CreateDb();
        var room = await SeedRoomAsync(db);
        var sut = new PlayerRatingService(db, AllowAllAuth().Object);

        await sut.RatePlayerAsync(room.Id, "Player", "home", 0.0m, "user-1");

        db.PlayerRatings.Single().Rating.Should().Be(1.0m);
    }

    [Fact]
    public async Task RatePlayerAsync_ClampsToMax_WhenAbove10()
    {
        using var db = CreateDb();
        var room = await SeedRoomAsync(db);
        var sut = new PlayerRatingService(db, AllowAllAuth().Object);

        await sut.RatePlayerAsync(room.Id, "Player", "home", 15.0m, "user-1");

        db.PlayerRatings.Single().Rating.Should().Be(10.0m);
    }

    [Fact]
    public async Task GetRatingsAsync_ComputesCorrectAverage()
    {
        using var db = CreateDb();
        var room = await SeedRoomAsync(db);
        var sut = new PlayerRatingService(db, AllowAllAuth().Object);

        await sut.RatePlayerAsync(room.Id, "Player", "home", 8.0m, "user-1");
        await sut.RatePlayerAsync(room.Id, "Player", "home", 6.0m, "user-2");

        var ratings = await sut.GetRatingsAsync(room.Id, null);

        var playerRating = ratings.Should().ContainSingle(r => r.PlayerName == "Player").Subject;
        playerRating.AverageRating.Should().Be(7.0m);
        playerRating.RatingCount.Should().Be(2);
    }

    [Fact]
    public async Task GetRatingsAsync_IncludesMyRating_ForRequestingUser()
    {
        using var db = CreateDb();
        var room = await SeedRoomAsync(db);
        var sut = new PlayerRatingService(db, AllowAllAuth().Object);

        await sut.RatePlayerAsync(room.Id, "Player", "home", 7.5m, "user-1");
        await sut.RatePlayerAsync(room.Id, "Player", "home", 5.0m, "user-2");

        var ratings = await sut.GetRatingsAsync(room.Id, "user-1");

        var playerRating = ratings.Single(r => r.PlayerName == "Player");
        playerRating.MyRating.Should().Be(7.5m);
    }
}
