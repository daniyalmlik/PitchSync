using PitchSync.MatchService.Services;

namespace MatchService.Tests.Services;

[Trait("Category", "Unit")]
public sealed class PresenceTrackerTests
{
    [Fact]
    public void AddConnection_TracksUserInRoom()
    {
        var sut = new PresenceTracker();

        sut.AddConnection("room-1", "user-1", "Alice", "Arsenal", "conn-1");

        sut.GetOnlineUsers("room-1").Should().ContainSingle(u => u.UserId == "user-1");
    }

    [Fact]
    public void RemoveConnection_RemovesUser_AndReturnsRoomAndUserId()
    {
        var sut = new PresenceTracker();
        sut.AddConnection("room-1", "user-1", "Alice", null, "conn-1");

        var result = sut.RemoveConnection("conn-1");

        result.Should().NotBeNull();
        result!.Value.RoomId.Should().Be("room-1");
        result!.Value.UserId.Should().Be("user-1");
        sut.GetOnlineUsers("room-1").Should().BeEmpty();
    }

    [Fact]
    public void RemoveConnection_MultiTab_ReturnsNull_WhenConnectionsRemain()
    {
        var sut = new PresenceTracker();
        sut.AddConnection("room-1", "user-1", "Alice", null, "conn-1");
        sut.AddConnection("room-1", "user-1", "Alice", null, "conn-2");

        var result = sut.RemoveConnection("conn-1");

        result.Should().BeNull(); // user still has conn-2 open
        sut.GetOnlineUsers("room-1").Should().ContainSingle(u => u.UserId == "user-1");
    }

    [Fact]
    public void GetOnlineUsers_ReturnsOneEntryPerUser_ForMultiTab()
    {
        var sut = new PresenceTracker();
        sut.AddConnection("room-1", "user-1", "Alice", null, "conn-1");
        sut.AddConnection("room-1", "user-1", "Alice", null, "conn-2");

        var users = sut.GetOnlineUsers("room-1");

        users.Should().ContainSingle(u => u.UserId == "user-1");
    }

    [Fact]
    public void GetOnlineUsers_IncludesDisplayNameAndFavoriteTeam()
    {
        var sut = new PresenceTracker();
        sut.AddConnection("room-1", "user-1", "Alice", "Arsenal", "conn-1");

        var users = sut.GetOnlineUsers("room-1");

        var user = users.Single();
        user.DisplayName.Should().Be("Alice");
        user.FavoriteTeam.Should().Be("Arsenal");
    }
}
