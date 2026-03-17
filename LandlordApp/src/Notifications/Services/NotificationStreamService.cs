using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Lander.src.Notifications.Services;

public class NotificationStreamService
{
    private readonly ConcurrentDictionary<int, Channel<NotificationMessage>> _userChannels = new();
    
    public async IAsyncEnumerable<NotificationMessage> StreamNotificationsAsync(
        int userId, 
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = _userChannels.GetOrAdd(userId, 
            _ => Channel.CreateUnbounded<NotificationMessage>());
        
        try
        {
            await foreach (var notification in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return notification;
            }
        }
        finally
        {
            _userChannels.TryRemove(userId, out _);
        }
    }
    
    public async Task SendNotificationAsync(int userId, NotificationMessage message)
    {
        if (_userChannels.TryGetValue(userId, out var channel))
        {
            await channel.Writer.WriteAsync(message);
        }
    }
    
    public async Task BroadcastNotificationAsync(NotificationMessage message)
    {
        foreach (var channel in _userChannels.Values)
        {
            await channel.Writer.WriteAsync(message);
        }
    }
    
    public int GetActiveConnectionCount() => _userChannels.Count;
}

public record NotificationMessage(
    string Type,
    string Title,
    string Message,
    DateTime Timestamp,
    string? ActionUrl = null,
    object? Data = null
);
