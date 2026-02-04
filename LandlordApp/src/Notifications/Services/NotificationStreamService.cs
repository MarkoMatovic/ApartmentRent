using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Lander.src.Notifications.Services;

/// <summary>
/// .NET 10 Feature: Server-Sent Events (SSE) for real-time notifications
/// Lightweight alternative to SignalR for one-way server-to-client communication
/// </summary>
public class NotificationStreamService
{
    private readonly ConcurrentDictionary<int, Channel<NotificationMessage>> _userChannels = new();
    
    /// <summary>
    /// Stream notifications to a specific user
    /// </summary>
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
            // Cleanup when client disconnects
            _userChannels.TryRemove(userId, out _);
        }
    }
    
    /// <summary>
    /// Send a notification to a specific user
    /// </summary>
    public async Task SendNotificationAsync(int userId, NotificationMessage message)
    {
        if (_userChannels.TryGetValue(userId, out var channel))
        {
            await channel.Writer.WriteAsync(message);
        }
    }
    
    /// <summary>
    /// Broadcast notification to all connected users
    /// </summary>
    public async Task BroadcastNotificationAsync(NotificationMessage message)
    {
        foreach (var channel in _userChannels.Values)
        {
            await channel.Writer.WriteAsync(message);
        }
    }
    
    /// <summary>
    /// Get count of active connections
    /// </summary>
    public int GetActiveConnectionCount() => _userChannels.Count;
}

/// <summary>
/// Notification message structure for SSE
/// </summary>
public record NotificationMessage(
    string Type,
    string Title,
    string Message,
    DateTime Timestamp,
    string? ActionUrl = null,
    object? Data = null
);
