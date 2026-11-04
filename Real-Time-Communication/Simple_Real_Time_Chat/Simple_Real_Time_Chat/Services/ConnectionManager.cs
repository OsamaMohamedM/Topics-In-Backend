using System.Collections.Concurrent;

namespace Simple_Real_Time_Chat.Services;

public sealed class ConnectionManager : IConnectionManager
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _connections =
        new(StringComparer.OrdinalIgnoreCase);

    public void AddConnection(string userName, string connectionId)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(connectionId))
        {
            return;
        }

        var normalizedUserName = userName.Trim();
        var userConnections = _connections.GetOrAdd(
            normalizedUserName,
            _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal));

        userConnections.TryAdd(connectionId, default);
    }

    public void RemoveConnection(string userName, string connectionId)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(connectionId))
        {
            return;
        }

        var normalizedUserName = userName.Trim();
        if (!_connections.TryGetValue(normalizedUserName, out var userConnections))
        {
            return;
        }

        userConnections.TryRemove(connectionId, out _);

        if (userConnections.IsEmpty)
        {
            _connections.TryRemove(normalizedUserName, out _);
        }
    }

    public IReadOnlyCollection<string> GetConnections(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return Array.Empty<string>();
        }

        return _connections.TryGetValue(userName.Trim(), out var userConnections)
            ? userConnections.Keys.ToArray()
            : Array.Empty<string>();
    }
}