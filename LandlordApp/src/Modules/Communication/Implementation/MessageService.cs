using Lander.src.Common.Exceptions;
using Lander.src.Common.Validation;
using System.Security.Claims;
using System.Text.Json;
using Lander.Helpers;
using Lander.src.Common;
using Lander.src.Infrastructure.FileStorage;
using Lander.src.Infrastructure.Services;
using Lander.src.Modules.Communication.Dtos.Dto;
using Lander.src.Modules.Communication.Dtos.InputDto;
using Lander.src.Modules.Communication.Interfaces;
using Lander.src.Modules.Communication.Models;
using Lander.src.Modules.Communication.Hubs;
using Lander.src.Notifications;
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
    private readonly IHubContext<ChatHub> _notificationHubContext;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IFileStorageService _fileStorage;
    private readonly IdempotencyService _idempotencyService;
    private readonly ILogger<MessageService> _logger;
    private readonly IPresenceTracker _presence;

    public MessageService(
        CommunicationsContext context,
        UsersContext usersContext,
        IHttpContextAccessor httpContextAccessor,
        IEmailService emailService,
        IHubContext<ChatHub> chatHubContext,
        IHubContext<ChatHub> notificationHubContext,
        IWebHostEnvironment webHostEnvironment,
        IFileStorageService fileStorage,
        IdempotencyService idempotencyService,
        ILogger<MessageService> logger,
        IPresenceTracker presence)
    {
        _context = context;
        _usersContext = usersContext;
        _httpContextAccessor = httpContextAccessor;
        _emailService = emailService;
        _chatHubContext = chatHubContext;
        _notificationHubContext = notificationHubContext;
        _webHostEnvironment = webHostEnvironment;
        _fileStorage = fileStorage;
        _idempotencyService = idempotencyService;
        _logger = logger;
        _presence = presence;
    }

    public async Task<MessageDto?> SendMessageAsync(int senderId, int receiverId, string messageText, bool isSuperLike = false, string? idempotencyKey = null,
        string? fileUrl = null, string? fileName = null, long? fileSize = null, string? fileType = null)
    {
        if (idempotencyKey is not null &&
            await _idempotencyService.IsDuplicateAsync($"msg:{senderId}:{idempotencyKey}"))
            return null;

        // Validacija fileUrl — samo interni, RELATIVNI upload putevi su dozvoljeni.
        // Ranije se gledao samo AbsolutePath pa je "https://evil.com/uploads/x.jpg"
        // prolazio (host spoofing). Chat upload vraća relativni "/uploads/chat-files/...",
        // pa apsolutne URL-ove odbijamo u potpunosti.
        if (fileUrl != null)
        {
            var isValidPath = fileUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)
                || fileUrl.StartsWith("/api/v1/messages/files/", StringComparison.OrdinalIgnoreCase);
            if (Uri.TryCreate(fileUrl, UriKind.Absolute, out _) || !isValidPath || fileUrl.Contains(".."))
                throw new BusinessRuleException(BusinessErrorCodes.InvalidFileUrl);
        }

        if (await IsUserBlockedAsync(receiverId, senderId))
            throw new BusinessRuleException(BusinessErrorCodes.MessagingNotAllowed);

        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        // Super-Like: validate sender has tokens (read-only check before transaction)
        if (isSuperLike)
        {
            var senderUser = await _usersContext.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == senderId);
            if (senderUser == null || senderUser.TokenBalance < 1)
                throw new BusinessRuleException(BusinessErrorCodes.InsufficientTokens);
        }

        var callerGuid = Guid.TryParse(currentUserGuid, out var cg) ? cg : (Guid?)null;
        var safeText = HtmlSanitizationHelper.SanitizePlainText(messageText) ?? string.Empty;
        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            MessageText = safeText,
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
        await _context.RunInTransactionAsync(async () =>
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
                    });
        var users = await _usersContext.Users.AsNoTracking()
            .Where(u => u.UserId == senderId || u.UserId == receiverId)
            .ToDictionaryAsync(u => u.UserId);
        users.TryGetValue(senderId, out var sender);
        users.TryGetValue(receiverId, out var receiver);
        // E-mail only reaches people who are away: someone with the site open already sees the message
        // live (chat + bell), so a mail about it would just be noise.
        if (receiver != null && !string.IsNullOrEmpty(receiver.Email) && !_presence.IsOnline(receiverId))
        {
            var senderName = sender != null ? $"{sender.FirstName} {sender.LastName}" : "Unknown";
            var preview = safeText.Length > 100 ? safeText.Substring(0, 100) + "..." : safeText;
            try
            {
                await _emailService.SendNewMessageEmailAsync(receiver.Email, senderName, preview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send new-message email notification to receiver {ReceiverId}", receiverId);
            }
        }
        var messageDto = new MessageDto
        {
            MessageId = message.MessageId,
            SenderId = senderId,
            ReceiverId = receiverId,
            MessageText = safeText,
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

        // The bell: the receiver's notification group (same hub and connection as the chat)
        // The 5th argument lets the browser show this in the user's language and open this chat on click.
        var descriptor = NotificationDescriptor.Create(
            "new_message",
            new Dictionary<string, string> { ["name"] = messageDto.SenderName ?? string.Empty },
            $"/chat?userId={senderId}");
        await _notificationHubContext.Clients.Group(receiverId.ToString()).SendAsync("ReceiveNotification",
            "Nova poruka",
            $"Imate novu poruku od {messageDto.SenderName}",
            "info",
            string.Empty,
            descriptor);

        return messageDto;
    }
}
