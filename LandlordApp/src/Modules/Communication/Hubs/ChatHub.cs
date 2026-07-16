using Lander.src.Modules.Communication.Dtos.Dto;
using Lander.src.Modules.Communication.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Claims;

namespace Lander.src.Modules.Communication.Hubs;

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

        MessageDto? message;
        try
        {
            message = await _messageService.SendMessageAsync(senderId, receiverId, messageText);
        }
        catch (InvalidOperationException ex)
        {
            throw new HubException(ex.Message);
        }
        if (message is null) return;

        await Clients.Group($"user_{receiverId}").SendAsync("ReceiveMessage", new
        {
            message.MessageId,
            message.SenderId,
            message.ReceiverId,
            message.MessageText,
            message.SentAt,
            message.IsRead,
            message.SenderName,
            message.SenderProfilePicture,
            message.FileUrl,
            message.FileName,
            message.FileSize,
            message.FileType,
            message.IsSuperLike
        });
        await Clients.Group($"user_{senderId}").SendAsync("MessageSent", new
        {
            message.MessageId,
            message.SenderId,
            message.ReceiverId,
            message.MessageText,
            message.SentAt,
            message.IsRead,
            message.ReceiverName,
            message.ReceiverProfilePicture,
            message.FileUrl,
            message.FileName,
            message.FileSize,
            message.FileType,
            message.IsSuperLike
        });
    }

    public async Task MarkMessageAsRead(int messageId)
    {
        var callerId = GetCurrentUserId();

        if (!await _messageService.IsMessageRecipientAsync(messageId, callerId))
            throw new HubException("Unauthorized");

        await _messageService.MarkAsReadAsync(messageId);
        var message = await _messageService.GetMessageByIdAsync(messageId);
        if (message != null)
        {
            await Clients.Group($"user_{message.SenderId}").SendAsync("MessageRead", new
            {
                MessageId = messageId,
                ReadAt = DateTime.UtcNow
            });
        }
    }

    public async Task UserTyping(int receiverId)
    {
        var userId = GetCurrentUserId();
        await Clients.Group($"user_{receiverId}").SendAsync("UserTyping", new { userId });
    }
}
