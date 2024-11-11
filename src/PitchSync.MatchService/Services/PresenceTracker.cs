using System.Collections.Concurrent;
using PitchSync.Shared.DTOs;

namespace PitchSync.MatchService.Services;

public sealed class PresenceTracker : IPresenceTracker
{
    // roomId → userId → connectionIds
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, HashSet<string>>> _rooms = new();

    // connectionId → (roomId, userId)
    private readonly ConcurrentDictionary<string, (string RoomId, string UserId)> _connections = new();

    // userId → (DisplayName, FavoriteTeam?)
    private readonly ConcurrentDictionary<string, (string DisplayName, string? FavoriteTeam)> _userInfo = new();

    public void AddConnection(string roomId, string userId, string displayName, string? favoriteTeam, string connectionId)
    {
        var userMap = _rooms.GetOrAdd(roomId, _ => new ConcurrentDictionary<string, HashSet<string>>());
        var connSet = userMap.GetOrAdd(userId, _ => new HashSet<string>());

        lock (connSet)
            connSet.Add(connectionId);

        _userInfo[userId] = (displayName, favoriteTeam);
        _connections[connectionId] = (roomId, userId);
    }

    public (string RoomId, string UserId)? RemoveConnection(string connectionId)
    {
        if (!_connections.TryRemove(connectionId, out var info))
            return null;

        var (roomId, userId) = info;

        if (!_rooms.TryGetValue(roomId, out var userMap) ||
            !userMap.TryGetValue(userId, out var connSet))
            return null;

        bool fullyDisconnected;
        lock (connSet)
        {
            connSet.Remove(connectionId);
            fullyDisconnected = connSet.Count == 0;
        }

        if (!fullyDisconnected)
            return null;

        userMap.TryRemove(userId, out _);
        return (roomId, userId);
    }

    public List<OnlineUserDto> GetOnlineUsers(string roomId)
    {
        if (!_rooms.TryGetValue(roomId, out var userMap))
            return [];

        return userMap.Keys
            .Select(uid => _userInfo.TryGetValue(uid, out var info)
                ? new OnlineUserDto(uid, info.DisplayName, info.FavoriteTeam)
                : null)
            .OfType<OnlineUserDto>()
            .ToList();
    }
}
