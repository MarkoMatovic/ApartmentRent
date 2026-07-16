using Lander.Helpers;
using Lander.src.Modules.Communication.Dtos.Dto;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Communication.Implementation;
public partial class MessageService
{
    public async Task<ConversationMessagesDto> GetConversationAsync(int userId1, int userId2, int page = 1, int pageSize = 50)
    {
        var userIds = new[] { userId1, userId2 };
        var users = await _usersContext.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId);

        // If either participant has disabled chat history, return empty result
        var chatHistoryBlocked =
            (users.TryGetValue(userId1, out var u1) && !u1.ChatHistoryConsent) ||
            (users.TryGetValue(userId2, out var u2) && !u2.ChatHistoryConsent);

        if (chatHistoryBlocked)
            return new ConversationMessagesDto { TotalCount = 0, Page = page, PageSize = pageSize, Messages = [] };

        var query = _context.Messages
            .AsNoTracking()
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
                IsSuperLike = m.IsSuperLike,
                SentAt = m.SentAt ?? DateTime.UtcNow,
                IsRead = m.IsRead ?? false,
            })
            .ToListAsync();
        var now = DateTime.UtcNow;
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

                // Priority Inbox: flag when the recipient has an active subscription AND
                // the sender is email-verified (verified = trustworthy user).
                var senderVerified = users.TryGetValue(msg.SenderId, out var snd) && snd?.EmailVerifiedAt != null;
                var recipientHasPriority = receiver.PriorityInboxUntil.HasValue && receiver.PriorityInboxUntil > now;
                msg.IsPriority = senderVerified && recipientHasPriority;
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
            .AsNoTracking()
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

        // Load other users + the current user (for consent check)
        var allRelevantIds = otherUserIds.Concat([userId]).Distinct().ToList();
        var users = await _usersContext.Users.AsNoTracking()
            .Where(u => allRelevantIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId);

        var currentUserHasConsentDisabled = users.TryGetValue(userId, out var currentUser) && !currentUser.ChatHistoryConsent;

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

                var otherUserHasConsentDisabled = users.TryGetValue(conv.OtherUserId ?? 0, out var ou) && !ou.ChatHistoryConsent;
                if (conv.LastMessage != null && !currentUserHasConsentDisabled && !otherUserHasConsentDisabled)
                {
                    conversationDto.LastMessage = new MessageDto
                    {
                        MessageId = conv.LastMessage.MessageId,
                        SenderId = conv.LastMessage.SenderId ?? 0,
                        ReceiverId = conv.LastMessage.ReceiverId ?? 0,
                        MessageText = conv.LastMessage.MessageText,
                        IsSuperLike = conv.LastMessage.IsSuperLike,
                        SentAt = conv.LastMessage.SentAt ?? DateTime.UtcNow,
                        IsRead = conv.LastMessage.IsRead ?? false,
                    };
                }

                result.Add(conversationDto);
            }
        }

        return result.OrderByDescending(c => c.LastMessage?.SentAt).ToList();
    }

    public async Task<bool> IsMessageRecipientAsync(int messageId, int userId)
    {
        return await _context.Messages
            .AnyAsync(m => m.MessageId == messageId && m.ReceiverId == userId);
    }

    public async Task MarkAsReadAsync(int messageId)
    {
        var message = await _context.Messages.FindAsync(messageId);
        if (message != null && message.IsRead == false)
        {
            // RunInTransactionAsync wraps the work in the retrying execution strategy —
            // a manually started transaction throws with EnableRetryOnFailure enabled.
            await _context.RunInTransactionAsync(async () =>
            {
                message.IsRead = true;
                message.ModifiedDate = DateTime.UtcNow;
                await _context.SaveEntitiesAsync();
            });
        }
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.Messages
            .AsNoTracking()
            .Where(m => m.ReceiverId == userId && m.IsRead == false)
            .CountAsync();
    }

    public async Task<MessageDto?> GetMessageByIdAsync(int messageId)
    {
        var message = await _context.Messages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MessageId == messageId);
        if (message == null) return null;

        // Batch-load both participants in one query instead of two FindAsync calls
        var participantIds = new[] { message.SenderId, message.ReceiverId }
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var userMap = await _usersContext.Users
            .AsNoTracking()
            .Where(u => participantIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId);

        userMap.TryGetValue(message.SenderId ?? -1, out var sender);
        userMap.TryGetValue(message.ReceiverId ?? -1, out var receiver);

        return new MessageDto
        {
            MessageId = message.MessageId,
            SenderId = message.SenderId ?? 0,
            ReceiverId = message.ReceiverId ?? 0,
            MessageText = message.MessageText,
            SentAt = message.SentAt ?? DateTime.UtcNow,
            IsRead = message.IsRead ?? false,
            IsSuperLike = message.IsSuperLike,
            SenderName = sender != null ? $"{sender.FirstName} {sender.LastName}" : null,
            ReceiverName = receiver != null ? $"{receiver.FirstName} {receiver.LastName}" : null,
            SenderProfilePicture = sender?.ProfilePicture,
            ReceiverProfilePicture = receiver?.ProfilePicture
        };
    }

    public async Task DeleteConversationAsync(int userId, int otherUserId)
    {
        // RunInTransactionAsync wraps the work in the retrying execution strategy —
        // a manually started transaction throws with EnableRetryOnFailure enabled.
        await _context.RunInTransactionAsync(async () =>
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
        });
    }

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
                IsSuperLike = m.IsSuperLike,
                SentAt = m.SentAt ?? DateTime.UtcNow,
                IsRead = m.IsRead ?? false,
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
}
