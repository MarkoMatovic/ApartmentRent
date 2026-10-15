using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace Lander.src.Infrastructure.Services;

/// <summary>
/// Knows which users are on the site right now: a user is online while at least one of their
/// SignalR connections (chat or notification hub, i.e. any open tab) is alive. It lets the app skip
/// e-mails for things the user is already seeing live.
///
/// Held in memory, so it only knows connections to THIS server instance. Behind several instances an
/// online user can look offline here, which errs on the safe side: an extra e-mail, never a missed one.
/// </summary>
public interface IPresenceTracker
{
    void Connected(int userId, string connectionId);
    void Disconnected(string connectionId);
    bool IsOnline(int userId);
}

public sealed class PresenceTracker : IPresenceTracker
{
    private readonly object _gate = new();
    private readonly Dictionary<int, HashSet<string>> _connectionsByUser = new();
    private readonly Dictionary<string, int> _userByConnection = new();

    public void Connected(int userId, string connectionId)
    {
        lock (_gate)
        {
            if (!_connectionsByUser.TryGetValue(userId, out var connections))
                _connectionsByUser[userId] = connections = new HashSet<string>();

            connections.Add(connectionId);
            _userByConnection[connectionId] = userId;
        }
    }

    public void Disconnected(string connectionId)
    {
        lock (_gate)
        {
            if (!_userByConnection.Remove(connectionId, out var userId)) return;
            if (!_connectionsByUser.TryGetValue(userId, out var connections)) return;

            connections.Remove(connectionId);
            if (connections.Count == 0) _connectionsByUser.Remove(userId);
        }
    }

    public bool IsOnline(int userId)
    {
        lock (_gate)
            return _connectionsByUser.ContainsKey(userId);
    }
}

/// <summary>
/// Global SignalR filter feeding <see cref="IPresenceTracker"/> from every hub, so the hubs
/// themselves need no presence code.
/// </summary>
public sealed class PresenceHubFilter : IHubFilter
{
    private readonly IPresenceTracker _presence;

    public PresenceHubFilter(IPresenceTracker presence) => _presence = presence;

    public Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        if (int.TryParse(context.Context.User?.FindFirstValue("userId"), out var userId))
            _presence.Connected(userId, context.Context.ConnectionId);

        return next(context);
    }

    public Task OnDisconnectedAsync(
        HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next)
    {
        _presence.Disconnected(context.Context.ConnectionId);
        return next(context, exception);
    }
}
