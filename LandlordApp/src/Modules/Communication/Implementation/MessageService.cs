using System.Security.Claims;
using Lander.src.Modules.Communication.Dtos.Dto;
using Lander.src.Modules.Communication.Dtos.InputDto;
using Lander.src.Modules.Communication.Intefaces;
using Lander.src.Modules.Communication.Models;
using Lander.src.Modules.Communication.Hubs;
using Lander.src.Notifications.NotificationsHub;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
namespace Lander.src.Modules.Communication.Implementation;
public class MessageService : IMessageService
{
    private readonly CommunicationsContext _context;
    private readonly UsersContext _usersContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEmailService _emailService;
    private readonly IHubContext<ChatHub> _chatHubContext;
    private readonly IHubContext<NotificationHub> _notificationHubContext;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public MessageService(
        CommunicationsContext context, 
        UsersContext usersContext, 
        IHttpContextAccessor httpContextAccessor,
        IEmailService emailService,
        IHubContext<ChatHub> chatHubContext,
        IHubContext<NotificationHub> notificationHubContext,
        IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _usersContext = usersContext;
        _httpContextAccessor = httpContextAccessor;
        _emailService = emailService;
        _chatHubContext = chatHubContext;
        _notificationHubContext = notificationHubContext;
        _webHostEnvironment = webHostEnvironment;
    }
    public async Task<MessageDto> SendMessageAsync(int senderId, int receiverId, string messageText)
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            MessageText = messageText,
            SentAt = DateTime.UtcNow,
            IsRead = false,
            CreatedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null,
            CreatedDate = DateTime.UtcNow,
            ModifiedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null,
            ModifiedDate = DateTime.UtcNow
        };
        var transaction = await _context.BeginTransactionAsync();
        try
        {
            _context.Messages.Add(message);
            await _context.SaveEntitiesAsync();
            await _context.CommitTransactionAsync(transaction);
        }
        catch
        {
            _context.RollBackTransaction();
            throw;
        }
        var sender = await _usersContext.Users.FindAsync(senderId);
        var receiver = await _usersContext.Users.FindAsync(receiverId);
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
    public async Task<ConversationMessagesDto> GetConversationAsync(int userId1, int userId2, int page = 1, int pageSize = 50)
    {
        var query = _context.Messages
            .Where(m => (m.SenderId == userId1 && m.ReceiverId == userId2) || 
                       (m.SenderId == userId2 && m.ReceiverId == userId1))
            .OrderByDescending(m => m.SentAt);
        var totalCount = await query.CountAsync();
        var messages = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MessageDto
            {
                MessageId = m.MessageId,
                SenderId = m.SenderId ?? 0,
                ReceiverId = m.ReceiverId ?? 0,
                MessageText = m.MessageText,
                SentAt = m.SentAt ?? DateTime.UtcNow,
                IsRead = m.IsRead ?? false
            })
            .ToListAsync();
        var userIds = new[] { userId1, userId2 };
        var users = await _usersContext.Users
            .Where(u => userIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId);
        foreach (var msg in messages)
        {
            if (users.TryGetValue(msg.SenderId, out var sender))
            {
                msg.SenderName = $"{sender.FirstName} {sender.LastName}";
                msg.SenderProfilePicture = sender.ProfilePicture;
            }
            if (users.TryGetValue(msg.ReceiverId, out var receiver))
            {
                msg.ReceiverName = $"{receiver.FirstName} {receiver.LastName}";
                msg.ReceiverProfilePicture = receiver.ProfilePicture;
            }
        }
        return new ConversationMessagesDto
        {
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            Messages = messages.OrderBy(m => m.SentAt).ToList()
        };
    }
    public async Task<List<ConversationDto>> GetUserConversationsAsync(int userId)
    {
        var conversations = await _context.Messages
            .Where(m => m.SenderId == userId || m.ReceiverId == userId)
            .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
            .Select(g => new
            {
                OtherUserId = g.Key,
                LastMessage = g.OrderByDescending(m => m.SentAt).FirstOrDefault(),
                UnreadCount = g.Count(m => m.ReceiverId == userId && m.IsRead == false)
            })
            .ToListAsync();

        var result = new List<ConversationDto>();
        var otherUserIds = conversations.Select(c => c.OtherUserId).ToList();
        var users = await _usersContext.Users
            .Where(u => otherUserIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId);

        // Load conversation settings for all conversations
        var settings = await _context.ConversationSettings
            .Where(s => s.UserId == userId && otherUserIds.Contains(s.OtherUserId))
            .ToDictionaryAsync(s => s.OtherUserId);

        foreach (var conv in conversations)
        {
            if (users.TryGetValue(conv.OtherUserId ?? 0, out var otherUser))
            {
                var conversationDto = new ConversationDto
                {
                    OtherUserId = conv.OtherUserId ?? 0,
                    OtherUserName = $"{otherUser.FirstName} {otherUser.LastName}",
                    OtherUserProfilePicture = otherUser.ProfilePicture,
                    UnreadCount = conv.UnreadCount,
                    IsArchived = false,
                    IsMuted = false,
                    IsBlocked = false
                };

                // Apply settings if they exist
                if (settings.TryGetValue(conv.OtherUserId ?? 0, out var setting))
                {
                    conversationDto.IsArchived = setting.IsArchived;
                    conversationDto.IsMuted = setting.IsMuted;
                    conversationDto.IsBlocked = setting.IsBlocked;
                }

                if (conv.LastMessage != null)
                {
                    conversationDto.LastMessage = new MessageDto
                    {
                        MessageId = conv.LastMessage.MessageId,
                        SenderId = conv.LastMessage.SenderId ?? 0,
                        ReceiverId = conv.LastMessage.ReceiverId ?? 0,
                        MessageText = conv.LastMessage.MessageText,
                        SentAt = conv.LastMessage.SentAt ?? DateTime.UtcNow,
                        IsRead = conv.LastMessage.IsRead ?? false
                    };
                }

                result.Add(conversationDto);
            }
        }

        return result.OrderByDescending(c => c.LastMessage?.SentAt).ToList();
    }
    public async Task MarkAsReadAsync(int messageId)
    {
        var message = await _context.Messages.FindAsync(messageId);
        if (message != null && message.IsRead == false)
        {
            var transaction = await _context.BeginTransactionAsync();
            try
            {
                message.IsRead = true;
                message.ModifiedDate = DateTime.UtcNow;
                await _context.SaveEntitiesAsync();
                await _context.CommitTransactionAsync(transaction);
            }
            catch
            {
                _context.RollBackTransaction();
                throw;
            }
        }
    }
    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.Messages
            .Where(m => m.ReceiverId == userId && m.IsRead == false)
            .CountAsync();
    }
    public async Task<MessageDto?> GetMessageByIdAsync(int messageId)
    {
        var message = await _context.Messages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MessageId == messageId);
        if (message == null) return null;
        var sender = message.SenderId.HasValue ? await _usersContext.Users.FindAsync(message.SenderId.Value) : null;
        var receiver = message.ReceiverId.HasValue ? await _usersContext.Users.FindAsync(message.ReceiverId.Value) : null;
        return new MessageDto
        {
            MessageId = message.MessageId,
            SenderId = message.SenderId ?? 0,
            ReceiverId = message.ReceiverId ?? 0,
            MessageText = message.MessageText,
            SentAt = message.SentAt ?? DateTime.UtcNow,
            IsRead = message.IsRead ?? false,
            SenderName = sender != null ? $"{sender.FirstName} {sender.LastName}" : null,
            ReceiverName = receiver != null ? $"{receiver.FirstName} {receiver.LastName}" : null,
            SenderProfilePicture = sender?.ProfilePicture,
            ReceiverProfilePicture = receiver?.ProfilePicture
        };
    }

    #region Conversation Settings

    private async Task<ConversationSettings> GetOrCreateSettingsAsync(int userId, int otherUserId)
    {
        var settings = await _context.ConversationSettings
            .FirstOrDefaultAsync(s => s.UserId == userId && s.OtherUserId == otherUserId);

        if (settings == null)
        {
            var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            settings = new ConversationSettings
            {
                UserId = userId,
                OtherUserId = otherUserId,
                IsArchived = false,
                IsMuted = false,
                IsBlocked = false,
                CreatedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null,
                CreatedDate = DateTime.UtcNow
            };
            _context.ConversationSettings.Add(settings);
            await _context.SaveEntitiesAsync();
        }

        return settings;
    }

    public async Task ArchiveConversationAsync(int userId, int otherUserId)
    {
        var settings = await GetOrCreateSettingsAsync(userId, otherUserId);
        settings.IsArchived = true;
        settings.ModifiedDate = DateTime.UtcNow;
        await _context.SaveEntitiesAsync();
    }

    public async Task UnarchiveConversationAsync(int userId, int otherUserId)
    {
        var settings = await GetOrCreateSettingsAsync(userId, otherUserId);
        settings.IsArchived = false;
        settings.ModifiedDate = DateTime.UtcNow;
        await _context.SaveEntitiesAsync();
    }

    public async Task MuteConversationAsync(int userId, int otherUserId)
    {
        var settings = await GetOrCreateSettingsAsync(userId, otherUserId);
        settings.IsMuted = true;
        settings.ModifiedDate = DateTime.UtcNow;
        await _context.SaveEntitiesAsync();
    }

    public async Task UnmuteConversationAsync(int userId, int otherUserId)
    {
        var settings = await GetOrCreateSettingsAsync(userId, otherUserId);
        settings.IsMuted = false;
        settings.ModifiedDate = DateTime.UtcNow;
        await _context.SaveEntitiesAsync();
    }

    public async Task BlockUserAsync(int userId, int blockedUserId)
    {
        var settings = await GetOrCreateSettingsAsync(userId, blockedUserId);
        settings.IsBlocked = true;
        settings.ModifiedDate = DateTime.UtcNow;
        await _context.SaveEntitiesAsync();
    }

    public async Task UnblockUserAsync(int userId, int blockedUserId)
    {
        var settings = await GetOrCreateSettingsAsync(userId, blockedUserId);
        settings.IsBlocked = false;
        settings.ModifiedDate = DateTime.UtcNow;
        await _context.SaveEntitiesAsync();
    }

    public async Task<bool> IsUserBlockedAsync(int userId, int otherUserId)
    {
        var settings = await _context.ConversationSettings
            .FirstOrDefaultAsync(s => s.UserId == userId && s.OtherUserId == otherUserId);
        return settings?.IsBlocked ?? false;
    }

    #endregion

    #region Conversation Management

    public async Task DeleteConversationAsync(int userId, int otherUserId)
    {
        var transaction = await _context.BeginTransactionAsync();
        try
        {
            // Delete all messages between these users
            var messages = await _context.Messages
                .Where(m => (m.SenderId == userId && m.ReceiverId == otherUserId) ||
                           (m.SenderId == otherUserId && m.ReceiverId == userId))
                .ToListAsync();

            _context.Messages.RemoveRange(messages);

            // Delete conversation settings
            var settings = await _context.ConversationSettings
                .FirstOrDefaultAsync(s => s.UserId == userId && s.OtherUserId == otherUserId);
            if (settings != null)
            {
                _context.ConversationSettings.Remove(settings);
            }

            await _context.SaveEntitiesAsync();
            await _context.CommitTransactionAsync(transaction);
        }
        catch
        {
            _context.RollBackTransaction();
            throw;
        }
    }

    #endregion

    #region File Upload

    public async Task<string> UploadFileAsync(IFormFile file, int userId)
    {
        // Validate file
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty");

        if (file.Length > 3 * 1024 * 1024) // 3MB
            throw new ArgumentException("File size exceeds 3MB limit");

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".xls", ".xlsx" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            throw new ArgumentException("File type not allowed");

        // Create upload directory if it doesn't exist
        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "chat-files");
        Directory.CreateDirectory(uploadsFolder);

        // Generate unique filename
        var uniqueFileName = $"{userId}_{DateTime.UtcNow.Ticks}{extension}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Return relative URL
        return $"/uploads/chat-files/{uniqueFileName}";
    }

    #endregion

    #region Search

    public async Task<List<MessageDto>> SearchMessagesAsync(int userId, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<MessageDto>();

        var messages = await _context.Messages
            .Where(m => (m.SenderId == userId || m.ReceiverId == userId) &&
                       m.MessageText.Contains(query))
            .OrderByDescending(m => m.SentAt)
            .Take(50)
            .Select(m => new MessageDto
            {
                MessageId = m.MessageId,
                SenderId = m.SenderId ?? 0,
                ReceiverId = m.ReceiverId ?? 0,
                MessageText = m.MessageText,
                SentAt = m.SentAt ?? DateTime.UtcNow,
                IsRead = m.IsRead ?? false
            })
            .ToListAsync();

        // Load user names
        var userIds = messages.SelectMany(m => new[] { m.SenderId, m.ReceiverId }).Distinct().ToList();
        var users = await _usersContext.Users
            .Where(u => userIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId);

        foreach (var msg in messages)
        {
            if (users.TryGetValue(msg.SenderId, out var sender))
            {
                msg.SenderName = $"{sender.FirstName} {sender.LastName}";
                msg.SenderProfilePicture = sender.ProfilePicture;
            }
            if (users.TryGetValue(msg.ReceiverId, out var receiver))
            {
                msg.ReceiverName = $"{receiver.FirstName} {receiver.LastName}";
                msg.ReceiverProfilePicture = receiver.ProfilePicture;
            }
        }

        return messages;
    }

    #endregion

    #region Report Abuse

    public async Task ReportAbuseAsync(int reportedByUserId, ReportMessageDto reportDto)
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        
        var report = new ReportedMessage
        {
            MessageId = reportDto.MessageId,
            ReportedByUserId = reportedByUserId,
            ReportedUserId = reportDto.ReportedUserId,
            Reason = reportDto.Reason,
            Status = "Pending",
            CreatedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null,
            CreatedDate = DateTime.UtcNow
        };

        var transaction = await _context.BeginTransactionAsync();
        try
        {
            _context.ReportedMessages.Add(report);
            await _context.SaveEntitiesAsync();
            await _context.CommitTransactionAsync(transaction);
        }
        catch
        {
            _context.RollBackTransaction();
            throw;
        }
    }

    #endregion
}
