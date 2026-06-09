using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Lander.src.Notifications.Services;

/// <summary>
/// Server-Sent Events delivery channel for real-time notifications.
///
/// Memory-safety guarantees:
/// - Channel is BoundedChannel with DropOldest so a slow/stalled SSE consumer
///   cannot cause unbounded memory growth.
/// - Key is (userId, connectionId) so two browser tabs of the same user each
///   get their own channel and TryRemove on disconnect doesn't affect the other.
/// - Channel is completed (Writer.Complete) when the connection drops so the
///   reader loop terminates cleanly.
/// </summary>
public class NotificationStreamService
{
    private const int ChannelCapacity = 50;

    // Key: (userId, connectionId) — connectionId is a GUID the controller generates
    // per SSE request so multiple tabs of the same user work independently.
    private readonly ConcurrentDictionary<(int UserId, string ConnId), Channel<NotificationMessage>> _channels = new();

    public async IAsyncEnumerable<NotificationMessage> StreamNotificationsAsync(
        int userId,
        string connectionId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = _channels.GetOrAdd(
            (userId, connectionId),
            _ => Channel.CreateBounded<NotificationMessage>(
                new BoundedChannelOptions(ChannelCapacity)
                {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = true,
                    SingleWriter = false,
                }));

        try
        {
            await foreach (var notification in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return notification;
            }
        }
        finally
        {
            if (_channels.TryRemove((userId, connectionId), out var removed))
                removed.Writer.TryComplete();
        }
    }

    public async Task SendNotificationAsync(int userId, NotificationMessage message)
    {
        foreach (var kvp in _channels)
        {
            if (kvp.Key.UserId == userId)
                await kvp.Value.Writer.WriteAsync(message);
        }
    }

    public async Task BroadcastNotificationAsync(NotificationMessage message)
    {
        foreach (var channel in _channels.Values)
            await channel.Writer.WriteAsync(message);
    }

    public int GetActiveConnectionCount() => _channels.Count;
}

public record NotificationMessage(
    string Type,
    string Title,
    string Message,
    DateTime Timestamp,
    string? ActionUrl = null,
    object? Data = null
);
