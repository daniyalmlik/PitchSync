using PitchSync.Shared.DTOs;

namespace PitchSync.MatchService.Services;

public interface IPresenceTracker
{
    void AddConnection(string roomId, string userId, string displayName, string? favoriteTeam, string connectionId);

    /// <summary>
    /// Removes a connection. Returns (roomId, userId) when the user has fully disconnected
    /// from the room (no remaining tabs). Returns null when the user still has other connections.
    /// </summary>
    (string RoomId, string UserId)? RemoveConnection(string connectionId);

    List<OnlineUserDto> GetOnlineUsers(string roomId);
}
