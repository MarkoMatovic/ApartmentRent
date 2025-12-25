using System.Security.Claims;
using Lander.src.Modules.Communication.Dtos.Dto;
using Lander.src.Modules.Communication.Intefaces;
using Lander.src.Modules.Communication.Models;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Communication.Implementation;

public class MessageService : IMessageService
{
    private readonly CommunicationsContext _context;
    private readonly UsersContext _usersContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEmailService _emailService;

    public MessageService(
        CommunicationsContext context, 
        UsersContext usersContext, 
        IHttpContextAccessor httpContextAccessor,
        IEmailService emailService)
    {
        _context = context;
        _usersContext = usersContext;
        _httpContextAccessor = httpContextAccessor;
        _emailService = emailService;
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

        return new MessageDto
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

        var otherUserIds = conversations.Select(c => c.OtherUserId).Where(id => id.HasValue).Select(id => id!.Value).ToList();
        var users = await _usersContext.Users
            .Where(u => otherUserIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId);

        var result = new List<ConversationDto>();

        foreach (var conv in conversations)
        {
            if (conv.OtherUserId == null) continue;

            var otherUser = users.GetValueOrDefault(conv.OtherUserId.Value);
            
            result.Add(new ConversationDto
            {
                OtherUserId = conv.OtherUserId.Value,
                OtherUserName = otherUser != null ? $"{otherUser.FirstName} {otherUser.LastName}" : "Unknown",
                OtherUserProfilePicture = otherUser?.ProfilePicture,
                LastMessage = conv.LastMessage != null ? new MessageDto
                {
                    MessageId = conv.LastMessage.MessageId,
                    SenderId = conv.LastMessage.SenderId ?? 0,
                    ReceiverId = conv.LastMessage.ReceiverId ?? 0,
                    MessageText = conv.LastMessage.MessageText,
                    SentAt = conv.LastMessage.SentAt ?? DateTime.UtcNow,
                    IsRead = conv.LastMessage.IsRead ?? false
                } : null,
                UnreadCount = conv.UnreadCount
            });
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
}
