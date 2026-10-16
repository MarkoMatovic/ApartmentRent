using Lander.Helpers;
using Lander.src.Common;
using Lander.src.Common.Exceptions;
using Lander.src.Common.Validation;
using Lander.src.Infrastructure.Services;
using Lander.src.Modules.Communication.Dtos.Dto;
using Lander.src.Modules.Communication.Hubs;
using Lander.src.Modules.Communication.Interfaces;
using Lander.src.Modules.Communication.Models;
using Lander.src.Modules.Roommates.Interfaces;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Notifications;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Communication.Implementation;

public class ChatGroupService : IChatGroupService
{
    /// <summary>How much of the earlier one-to-one conversation is carried into a new group.</summary>
    private const int MaxImportedMessages = 500;

    private readonly CommunicationsContext _context;
    private readonly UsersContext _usersContext;
    private readonly IRoommatePairingService _pairings;
    private readonly IEmailService _emailService;
    private readonly IHubContext<ChatHub> _chatHub;
    private readonly IHubContext<ChatHub> _notificationHub;
    private readonly IPresenceTracker _presence;
    private readonly ILogger<ChatGroupService> _logger;

    public ChatGroupService(
        CommunicationsContext context,
        UsersContext usersContext,
        IRoommatePairingService pairings,
        IEmailService emailService,
        IHubContext<ChatHub> chatHub,
        IHubContext<ChatHub> notificationHub,
        IPresenceTracker presence,
        ILogger<ChatGroupService> logger)
    {
        _context = context;
        _usersContext = usersContext;
        _pairings = pairings;
        _emailService = emailService;
        _chatHub = chatHub;
        _notificationHub = notificationHub;
        _presence = presence;
        _logger = logger;
    }

    // ── Open / create ────────────────────────────────────────────────────────

    public async Task<ChatGroupDto> OpenWithRoommateAsync(int userId, int otherUserId)
    {
        var partnerId = await _pairings.GetActivePartnerIdAsync(userId)
            ?? throw new BusinessRuleException(BusinessErrorCodes.PairingRequired);

        if (otherUserId == userId || otherUserId == partnerId)
            throw new BusinessRuleException(BusinessErrorCodes.GroupChatNotAllowed);

        // The group only makes sense for a person one of the roommates already talks to, or whose apartment
        // is on the pair's shared list (that is why they would want to talk to the landlord together).
        var hasHistory = await _context.Messages.AsNoTracking().AnyAsync(m =>
            ((m.SenderId == userId || m.SenderId == partnerId) && m.ReceiverId == otherUserId)
            || (m.SenderId == otherUserId && (m.ReceiverId == userId || m.ReceiverId == partnerId)));
        if (!hasHistory && !await _pairings.IsLandlordOfSharedApartmentAsync(userId, otherUserId))
            throw new BusinessRuleException(BusinessErrorCodes.GroupChatNotAllowed);

        if (await IsBlockedEitherWayAsync(userId, otherUserId) || await IsBlockedEitherWayAsync(partnerId, otherUserId))
            throw new BusinessRuleException(BusinessErrorCodes.MessagingNotAllowed);

        var memberIds = new[] { userId, partnerId, otherUserId };

        var existing = await _context.ChatGroups.AsNoTracking()
            .Where(g => g.Members.Count == 3 && g.Members.Count(m => memberIds.Contains(m.UserId)) == 3)
            .Select(g => (int?)g.GroupId)
            .FirstOrDefaultAsync();
        if (existing.HasValue)
            return (await BuildGroupsAsync(userId, new List<int> { existing.Value })).First();

        var people = await _usersContext.Users.AsNoTracking()
            .Where(u => memberIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId);

        // Nothing earlier is copied for a person who keeps no chat history.
        var importHistory = memberIds.All(id => !people.TryGetValue(id, out var u) || u.ChatHistoryConsent);

        ChatGroup group = null!;
        await _context.RunInTransactionAsync(async () =>
        {
            group = new ChatGroup { CreatedByUserId = userId, CreatedDate = DateTime.UtcNow };
            _context.ChatGroups.Add(group);
            await _context.SaveEntitiesAsync();

            var lastImportedId = 0;
            if (importHistory)
            {
                var earlier = await _context.Messages.AsNoTracking()
                    .Where(m => ((m.SenderId == userId || m.SenderId == partnerId) && m.ReceiverId == otherUserId)
                                || (m.SenderId == otherUserId && (m.ReceiverId == userId || m.ReceiverId == partnerId)))
                    .OrderByDescending(m => m.SentAt)
                    .Take(MaxImportedMessages)
                    .ToListAsync();

                var copies = new List<ChatGroupMessage>();
                foreach (var message in earlier.OrderBy(m => m.SentAt))
                {
                    var text = !string.IsNullOrWhiteSpace(message.MessageText)
                        ? message.MessageText
                        : (message.FileName is null ? string.Empty : $"📎 {message.FileName}");
                    if (text.Length == 0 || message.SenderId is null) continue;

                    copies.Add(new ChatGroupMessage
                    {
                        GroupId = group.GroupId,
                        SenderId = message.SenderId.Value,
                        MessageText = text,
                        SentAt = message.SentAt ?? message.CreatedDate ?? DateTime.UtcNow,
                    });
                }
                if (copies.Count > 0)
                {
                    _context.ChatGroupMessages.AddRange(copies);
                    await _context.SaveEntitiesAsync();
                    lastImportedId = copies.Max(c => c.GroupMessageId);
                }
            }

            foreach (var memberId in memberIds)
            {
                _context.ChatGroupMembers.Add(new ChatGroupMember
                {
                    GroupId = group.GroupId,
                    UserId = memberId,
                    // What came with the group is background, not news.
                    LastReadMessageId = lastImportedId,
                    JoinedDate = DateTime.UtcNow,
                });
            }
            await _context.SaveEntitiesAsync();
        });

        // The roommate who was added (and the landlord) should find the new conversation straight away.
        foreach (var memberId in memberIds.Where(id => id != userId))
        {
            await PushAsync(memberId, "GroupCreated", new { GroupId = group.GroupId });
            await NotifyAsync(memberId, "group_created", NameOf(people, userId), "Novi grupni razgovor",
                $"{NameOf(people, userId)} vas je dodao/la u zajednički razgovor.");
        }

        return (await BuildGroupsAsync(userId, new List<int> { group.GroupId })).First();
    }

    // ── Read ─────────────────────────────────────────────────────────────────

    public async Task<List<ChatGroupDto>> GetUserGroupsAsync(int userId)
    {
        var groupIds = await _context.ChatGroupMembers.AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => m.GroupId)
            .ToListAsync();
        if (groupIds.Count == 0) return new List<ChatGroupDto>();

        var groups = await BuildGroupsAsync(userId, groupIds);
        return groups
            .OrderByDescending(g => g.LastMessage?.SentAt ?? DateTime.MinValue)
            .ToList();
    }

    public async Task<ChatGroupMessagesDto> GetMessagesAsync(int groupId, int userId, int page = 1, int pageSize = 50)
    {
        var memberIds = await RequireMemberAsync(groupId, userId);

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 50 : pageSize > 100 ? 100 : pageSize;

        var people = await _usersContext.Users.AsNoTracking()
            .Where(u => memberIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId);

        // Same rule as a one-to-one chat: if anybody keeps no chat history, nothing is shown.
        if (people.Values.Any(u => !u.ChatHistoryConsent))
            return new ChatGroupMessagesDto { Page = page, PageSize = pageSize };

        var query = _context.ChatGroupMessages.AsNoTracking()
            .Where(m => m.GroupId == groupId)
            .OrderByDescending(m => m.SentAt)
            .ThenByDescending(m => m.GroupMessageId);

        var total = await query.CountAsync();
        var rows = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new ChatGroupMessagesDto
        {
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            Messages = rows
                .OrderBy(m => m.SentAt).ThenBy(m => m.GroupMessageId)
                .Select(m => ToDto(m, people))
                .ToList(),
        };
    }

    // ── Write ────────────────────────────────────────────────────────────────

    public async Task<ChatGroupMessageDto> SendMessageAsync(int groupId, int userId, string messageText)
    {
        var memberIds = await RequireMemberAsync(groupId, userId);

        var safeText = HtmlSanitizationHelper.SanitizePlainText(messageText)?.Trim() ?? string.Empty;
        if (safeText.Length == 0)
            throw new BusinessRuleException(ValidationCodes.MessageEmpty);

        var message = new ChatGroupMessage
        {
            GroupId = groupId,
            SenderId = userId,
            MessageText = safeText,
            SentAt = DateTime.UtcNow,
        };

        await _context.RunInTransactionAsync(async () =>
        {
            _context.ChatGroupMessages.Add(message);
            await _context.SaveEntitiesAsync();

            // Your own message is never "unread" to you.
            var mine = await _context.ChatGroupMembers.FirstAsync(m => m.GroupId == groupId && m.UserId == userId);
            mine.LastReadMessageId = message.GroupMessageId;
            await _context.SaveEntitiesAsync();
        });

        var people = await _usersContext.Users.AsNoTracking()
            .Where(u => memberIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId);
        var dto = ToDto(message, people);

        // Anybody who blocked the sender is not bothered (they can still read the group).
        var recipients = new List<int>();
        foreach (var memberId in memberIds.Where(id => id != userId))
        {
            if (!await _context.ConversationSettings.AsNoTracking()
                    .AnyAsync(s => s.UserId == memberId && s.OtherUserId == userId && s.IsBlocked))
                recipients.Add(memberId);
        }

        // Every member (the sender's other tabs included) gets it live; the client drops duplicates by id.
        foreach (var memberId in memberIds)
            await PushAsync(memberId, "ReceiveGroupMessage", dto);

        var preview = safeText.Length > 100 ? safeText[..100] + "..." : safeText;
        foreach (var recipientId in recipients)
        {
            await NotifyAsync(recipientId, "group_message", dto.SenderName ?? string.Empty, "Nova poruka u grupi",
                $"{dto.SenderName} je napisao/la u zajedničkom razgovoru.", groupId);

            // Mail only reaches people who are away; whoever is on the site already sees it live.
            if (_presence.IsOnline(recipientId) || !people.TryGetValue(recipientId, out var recipient) || string.IsNullOrEmpty(recipient.Email))
                continue;
            try
            {
                await _emailService.SendNewMessageEmailAsync(recipient.Email, dto.SenderName ?? "TuRentaj", preview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send group-message email to user {UserId}", recipientId);
            }
        }

        return dto;
    }

    public async Task MarkReadAsync(int groupId, int userId)
    {
        await RequireMemberAsync(groupId, userId);

        var newest = await _context.ChatGroupMessages.AsNoTracking()
            .Where(m => m.GroupId == groupId)
            .MaxAsync(m => (int?)m.GroupMessageId) ?? 0;

        var member = await _context.ChatGroupMembers.FirstAsync(m => m.GroupId == groupId && m.UserId == userId);
        if (member.LastReadMessageId >= newest) return;

        member.LastReadMessageId = newest;
        await _context.RunInTransactionAsync(() => _context.SaveEntitiesAsync());
    }

    public async Task LeaveAsync(int groupId, int userId)
    {
        await RequireMemberAsync(groupId, userId);

        var member = await _context.ChatGroupMembers.FirstAsync(m => m.GroupId == groupId && m.UserId == userId);
        _context.ChatGroupMembers.Remove(member);
        await _context.RunInTransactionAsync(() => _context.SaveEntitiesAsync());
    }

    public Task<int> GetUnreadCountAsync(int userId) =>
        _context.UnreadGroupMessages(userId).CountAsync();

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Throws unless the user is in the group; returns everybody in it.</summary>
    private async Task<List<int>> RequireMemberAsync(int groupId, int userId)
    {
        var memberIds = await _context.ChatGroupMembers.AsNoTracking()
            .Where(m => m.GroupId == groupId)
            .Select(m => m.UserId)
            .ToListAsync();

        if (memberIds.Count == 0) throw new NotFoundException("ChatGroup", groupId);
        if (!memberIds.Contains(userId)) throw new ForbiddenException("You are not a member of this conversation.");
        return memberIds;
    }

    private async Task<List<ChatGroupDto>> BuildGroupsAsync(int viewerId, List<int> groupIds)
    {
        var members = await _context.ChatGroupMembers.AsNoTracking()
            .Where(m => groupIds.Contains(m.GroupId))
            .ToListAsync();
        var userIds = members.Select(m => m.UserId).Distinct().ToList();
        var people = await _usersContext.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId);

        var lastIds = await _context.ChatGroupMessages.AsNoTracking()
            .Where(m => groupIds.Contains(m.GroupId))
            .GroupBy(m => m.GroupId)
            .Select(g => g.Max(m => m.GroupMessageId))
            .ToListAsync();
        var lastMessages = (await _context.ChatGroupMessages.AsNoTracking()
                .Where(m => lastIds.Contains(m.GroupMessageId))
                .ToListAsync())
            .ToDictionary(m => m.GroupId);

        var unread = (await _context.UnreadGroupMessages(viewerId)
                .Where(m => groupIds.Contains(m.GroupId))
                .GroupBy(m => m.GroupId)
                .Select(g => new { GroupId = g.Key, Count = g.Count() })
                .ToListAsync())
            .ToDictionary(x => x.GroupId, x => x.Count);

        return groupIds.Select(groupId =>
        {
            lastMessages.TryGetValue(groupId, out var last);
            // Anyone keeping no chat history hides the preview, as in one-to-one chats.
            var historyHidden = members
                .Where(m => m.GroupId == groupId)
                .Any(m => people.TryGetValue(m.UserId, out var p) && !p.ChatHistoryConsent);
            return new ChatGroupDto
            {
                GroupId = groupId,
                Members = members.Where(m => m.GroupId == groupId)
                    .Select(m => people.TryGetValue(m.UserId, out var p)
                        ? new ChatGroupMemberDto { UserId = p.UserId, Name = FullName(p), ProfilePicture = p.ProfilePicture }
                        : new ChatGroupMemberDto { UserId = m.UserId })
                    .ToList(),
                LastMessage = last is null || historyHidden ? null : ToDto(last, people),
                UnreadCount = unread.GetValueOrDefault(groupId),
            };
        }).ToList();
    }

    private async Task<bool> IsBlockedEitherWayAsync(int a, int b) =>
        await _context.ConversationSettings.AsNoTracking().AnyAsync(s =>
            s.IsBlocked && ((s.UserId == a && s.OtherUserId == b) || (s.UserId == b && s.OtherUserId == a)));

    private static string FullName(User user) => $"{user.FirstName} {user.LastName}".Trim();

    private static string NameOf(Dictionary<int, User> people, int userId) =>
        people.TryGetValue(userId, out var user) ? FullName(user) : "Neko";

    private static ChatGroupMessageDto ToDto(ChatGroupMessage message, Dictionary<int, User> people)
    {
        people.TryGetValue(message.SenderId, out var sender);
        return new ChatGroupMessageDto
        {
            MessageId = message.GroupMessageId,
            GroupId = message.GroupId,
            SenderId = message.SenderId,
            SenderName = sender is null ? null : FullName(sender),
            SenderProfilePicture = sender?.ProfilePicture,
            MessageText = message.MessageText,
            SentAt = message.SentAt,
        };
    }

    /// <summary>A failed live push must never fail the message: it is already saved and shows on the next load.</summary>
    private async Task PushAsync(int userId, string method, object payload)
    {
        try
        {
            await _chatHub.Clients.Group($"user_{userId}").SendAsync(method, payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not push {Method} to user {UserId}", method, userId);
        }
    }

    /// <summary>
    /// Title and message are the Serbian fallback; <paramref name="key"/> and the name let the browser show
    /// the notification in the user's language, and a click opens the chat (the group, when one is given).
    /// </summary>
    private async Task NotifyAsync(int userId, string key, string name, string title, string message, int? groupId = null)
    {
        try
        {
            var link = groupId is null ? "/chat" : $"/chat?group={groupId}";
            var descriptor = NotificationDescriptor.Create(key, new Dictionary<string, string> { ["name"] = name }, link);
            await _notificationHub.Clients.Group(userId.ToString())
                .SendAsync("ReceiveNotification", title, message, "info", string.Empty, descriptor);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not push a notification to user {UserId}", userId);
        }
    }
}
