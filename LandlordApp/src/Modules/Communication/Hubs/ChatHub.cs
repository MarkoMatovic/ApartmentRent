using System.Collections.Concurrent;
using Lander.src.Modules.Communication.Dtos.Dto;
using Lander.src.Modules.Communication.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Lander.src.Modules.Communication.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMessageService _messageService;

    // Per-user sliding window: max 30 poruka u 60 sekundi (S-13 fix)
    // ConcurrentDictionary<userId, (windowStart, count)>
    private static readonly ConcurrentDictionary<int, (DateTime WindowStart, int Count)> _messageCounts = new();
    private const int MessageRateLimitPerMinute = 30;
    private const int MaxMessageLength = 4000;

    public ChatHub(IMessageService messageService)
    {
        _messageService = messageService;
    }

    private int GetCurrentUserId()
    {
        var claim = Context.User?.FindFirstValue("userId");
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id))
            throw new HubException("Unauthorized");
        return id;
    }

    private static void EnforceMessageRateLimit(int userId)
    {
        var now = DateTime.UtcNow;
        _messageCounts.AddOrUpdate(
            userId,
            _ => (now, 1),
            (_, existing) =>
            {
                if ((now - existing.WindowStart).TotalSeconds >= 60)
                    return (now, 1);
                if (existing.Count >= MessageRateLimitPerMinute)
                    throw new HubException("Previše poruka — pričekajte trenutak.");
                return (existing.WindowStart, existing.Count + 1);
            });
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
        EnforceMessageRateLimit(senderId);

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

        // Samo primatelj može označiti poruku kao pročitanu (S-3 fix)
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

    public async Task UserTyping(int userId, int receiverId)
    {
        await Clients.Group($"user_{receiverId}").SendAsync("UserTyping", new { userId });
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Čišćenje rate limit entrija pri diskonektu
        var claim = Context.User?.FindFirstValue("userId");
        if (int.TryParse(claim, out var userId))
            _messageCounts.TryRemove(userId, out _);

        await base.OnDisconnectedAsync(exception);
    }
}
