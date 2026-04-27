using System.Security.Claims;
using System.Text.Json;
using Lander.Helpers;
using Lander.src.Modules.Communication.Dtos.Dto;
using Lander.src.Modules.Communication.Dtos.InputDto;
using Lander.src.Modules.Communication.Interfaces;
using Lander.src.Modules.Communication.Models;
using Lander.src.Modules.Communication.Hubs;
using Lander.src.Notifications.NotificationsHub;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;

namespace Lander.src.Modules.Communication.Implementation;
public partial class MessageService : IMessageService
{
    private readonly CommunicationsContext _context;
    private readonly UsersContext _usersContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEmailService _emailService;
    private readonly IHubContext<ChatHub> _chatHubContext;
    private readonly IHubContext<NotificationHub> _notificationHubContext;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IdempotencyService _idempotencyService;

    public MessageService(
        CommunicationsContext context,
        UsersContext usersContext,
        IHttpContextAccessor httpContextAccessor,
        IEmailService emailService,
        IHubContext<ChatHub> chatHubContext,
        IHubContext<NotificationHub> notificationHubContext,
        IWebHostEnvironment webHostEnvironment,
        IdempotencyService idempotencyService)
    {
        _context = context;
        _usersContext = usersContext;
        _httpContextAccessor = httpContextAccessor;
        _emailService = emailService;
        _chatHubContext = chatHubContext;
        _notificationHubContext = notificationHubContext;
        _webHostEnvironment = webHostEnvironment;
        _idempotencyService = idempotencyService;
    }

    public async Task<MessageDto?> SendMessageAsync(int senderId, int receiverId, string messageText, bool isSuperLike = false, string? idempotencyKey = null,
        string? fileUrl = null, string? fileName = null, long? fileSize = null, string? fileType = null)
    {
        if (idempotencyKey is not null &&
            await _idempotencyService.IsDuplicateAsync($"msg:{senderId}:{idempotencyKey}"))
            return null;

        if (await IsUserBlockedAsync(receiverId, senderId))
            throw new InvalidOperationException("You cannot send messages to this user.");

        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        // Super-Like: validate sender has tokens (read-only check before transaction)
        if (isSuperLike)
        {
            var senderUser = await _usersContext.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == senderId);
            if (senderUser == null || senderUser.TokenBalance < 1)
                throw new InvalidOperationException("Nemate dovoljno tokena za Super-Like.");
        }

        var callerGuid = Guid.TryParse(currentUserGuid, out var cg) ? cg : (Guid?)null;
        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            MessageText = messageText,
            IsSuperLike = isSuperLike,
            FileUrl = fileUrl,
            FileName = fileName,
            FileSize = fileSize,
            FileType = fileType,
            SentAt = DateTime.UtcNow,
            IsRead = false,
            CreatedByGuid = callerGuid,
            CreatedDate = DateTime.UtcNow,
            ModifiedByGuid = callerGuid,
            ModifiedDate = DateTime.UtcNow
        };
        // Outbox pattern: message + token deduction event are written atomically in one transaction.
        // OutboxProcessorService picks up the event and deducts tokens from UsersContext separately,
        // guaranteeing at-least-once delivery without a distributed transaction.
        var transaction = await _context.BeginTransactionAsync();
        try
        {
            _context.Messages.Add(message);

            if (isSuperLike)
            {
                _context.OutboxMessages.Add(new OutboxMessage
                {
                    EventType = "SuperLikeTokenDeduction",
                    Payload = JsonSerializer.Serialize(new { UserId = senderId }),
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveEntitiesAsync();
            await _context.CommitTransactionAsync(transaction);
        }
        catch
        {
            _context.RollBackTransaction();
            throw;
        }
        var users = await _usersContext.Users.AsNoTracking()
            .Where(u => u.UserId == senderId || u.UserId == receiverId)
            .ToDictionaryAsync(u => u.UserId);
        users.TryGetValue(senderId, out var sender);
        users.TryGetValue(receiverId, out var receiver);
        if (receiver != null && !string.IsNullOrEmpty(receiver.Email))
        {
            var senderName = sender != null ? $"{sender.FirstName} {sender.LastName}" : "Unknown";
            var preview = messageText.Length > 100 ? messageText.Substring(0, 100) + "..." : messageText;
            _ = _emailService.SendNewMessageEmailAsync(receiver.Email, senderName, preview);
        }
        var messageDto = new MessageDto
        {
            MessageId = message.MessageId,
            SenderId = senderId,
            ReceiverId = receiverId,
            MessageText = messageText,
            IsSuperLike = isSuperLike,
            FileUrl = fileUrl,
            FileName = fileName,
            FileSize = fileSize,
            FileType = fileType,
            SentAt = message.SentAt ?? DateTime.UtcNow,
            IsRead = message.IsRead ?? false,
            SenderName = sender != null ? $"{sender.FirstName} {sender.LastName}" : null,
            ReceiverName = receiver != null ? $"{receiver.FirstName} {receiver.LastName}" : null,
            SenderProfilePicture = sender?.ProfilePicture,
            ReceiverProfilePicture = receiver?.ProfilePicture
        };

        // Broadcast to ChatHub (Real-time Chat)
        await _chatHubContext.Clients.Group($"user_{receiverId}").SendAsync("ReceiveMessage", messageDto);
        await _chatHubContext.Clients.Group($"user_{senderId}").SendAsync("MessageSent", messageDto);

        // Broadcast to NotificationHub (Global Notification Bell)
        await _notificationHubContext.Clients.Group(receiverId.ToString()).SendAsync("ReceiveNotification",
            "New Message",
            $"You have a new message from {messageDto.SenderName}",
            "info");

        return messageDto;
    }
}
