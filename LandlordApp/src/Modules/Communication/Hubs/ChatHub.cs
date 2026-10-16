using Lander.src.Modules.Communication.Dtos.Dto;
using Lander.src.Modules.Communication.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Claims;

namespace Lander.src.Modules.Communication.Hubs;

/// <summary>
/// The one live connection of a browser tab: chat, and the notifications of the bell (which had their own
/// hub, so every tab held two connections). Also mapped at /notificationHub for tabs opened before that.
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly IMessageService _messageService;
    private readonly IDistributedCache _cache;

    private const int MessageRateLimitPerMinute = 30;
    private const int MaxMessageLength = 4000;

    public ChatHub(IMessageService messageService, IDistributedCache cache)
    {
        _messageService = messageService;
        _cache = cache;
    }

    /// <summary>The group the bell's notifications are sent to (the user id).</summary>
    public static string NotificationGroup(int userId) => userId.ToString();

    /// <summary>
    /// Puts every new connection into its owner's chat room and notification group right away (the user id
    /// comes from the token), so nothing that arrives before the client has asked to join, or after an
    /// automatic reconnect, is lost.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        if (int.TryParse(Context.User?.FindFirstValue("userId"), out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            await Groups.AddToGroupAsync(Context.ConnectionId, NotificationGroup(userId));
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Joins the caller to their own notification group (tabs opened before the hubs were merged still ask).
    /// The user id is taken from the token; asking for somebody else's group closes the connection.
    /// </summary>
    public async Task JoinNotificationGroup(int userId)
    {
        if (!int.TryParse(Context.User?.FindFirstValue("userId"), out var callerId) || callerId != userId)
        {
            Context.Abort();
            return;
        }
        await Groups.AddToGroupAsync(Context.ConnectionId, NotificationGroup(callerId));
    }

    /// <summary>Admin-only broadcast to everybody on the site.</summary>
    [Authorize(Roles = "Admin")]
    public async Task SendNotificationToAll(string message)
    {
        await Clients.All.SendAsync("ReceiveNotification", message);
    }

    private int GetCurrentUserId()
    {
        var claim = Context.User?.FindFirstValue("userId");
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id))
            throw new HubException("Unauthorized");
        return id;
    }

    // Uses IDistributedCache (Redis in prod, in-memory in dev) so the rate limit
    // is shared across all horizontally scaled instances.
    private async Task EnforceMessageRateLimitAsync(int userId)
    {
        var minute = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60;
        var key = $"chat:rl:{userId}:{minute}";

        var raw = await _cache.GetStringAsync(key);
        // TryParse — a missing or corrupted cache entry must never break messaging
        var count = int.TryParse(raw, out var parsed) ? parsed : 0;

        if (count >= MessageRateLimitPerMinute)
            throw new HubException("Previše poruka — pričekajte trenutak.");

        await _cache.SetStringAsync(key, (count + 1).ToString(),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2) });
    }

    public async Task JoinChatRoom(int userId)
    {
        var callerId = GetCurrentUserId();
        if (callerId != userId) throw new HubException("Unauthorized");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
    }

    public async Task SendMessage(int receiverId, string messageText)
    {
        if (string.IsNullOrWhiteSpace(messageText))
            throw new HubException("Poruka ne može biti prazna.");
        if (messageText.Length > MaxMessageLength)
            throw new HubException($"Poruka ne smije biti duža od {MaxMessageLength} znakova.");

        var senderId = GetCurrentUserId();
        await EnforceMessageRateLimitAsync(senderId);

        // SendMessageAsync pushes "ReceiveMessage" and "MessageSent" itself (it is shared with the REST
        // path), so the hub sends nothing more: a second copy reached every client twice.
        try
        {
            await _messageService.SendMessageAsync(senderId, receiverId, messageText);
        }
        catch (InvalidOperationException ex)
        {
            throw new HubException(ex.Message);
        }
    }

    public async Task MarkMessageAsRead(int messageId)
    {
        var callerId = GetCurrentUserId();

        if (!await _messageService.IsMessageRecipientAsync(messageId, callerId))
            throw new HubException("Unauthorized");

        // MarkAsReadAsync pushes "MessageRead" to the sender itself (it is shared with the REST path).
        await _messageService.MarkAsReadAsync(messageId);
    }

    public async Task UserTyping(int receiverId)
    {
        var userId = GetCurrentUserId();
        await Clients.Group($"user_{receiverId}").SendAsync("UserTyping", new { userId });
    }
}
