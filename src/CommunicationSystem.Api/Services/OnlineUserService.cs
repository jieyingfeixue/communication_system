using System.Collections.Concurrent;

namespace CommunicationSystem.Api.Services;

public class OnlineUserService
{
    private readonly ConcurrentDictionary<int, HashSet<string>> _connections = new();

    public void UserConnected(int userId, string connectionId)
    {
        var set = _connections.GetOrAdd(userId, _ => []);
        lock (set) set.Add(connectionId);
    }

    public void UserDisconnected(int userId, string connectionId)
    {
        if (_connections.TryGetValue(userId, out var set))
        {
            lock (set) set.Remove(connectionId);
            if (set.Count == 0)
                _connections.TryRemove(userId, out _);
        }
    }

    public bool IsOnline(int userId) =>
        _connections.TryGetValue(userId, out var set) && set.Count > 0;

    public IReadOnlyCollection<int> GetOnlineUserIds() =>
        _connections.Keys.ToList();
}
