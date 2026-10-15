using System;
using System.Text.Json;
using Lander.Helpers;
using Lander.src.Notifications.Dtos.Dto;
using Lander.src.Notifications.Dtos.InputDto;
using Lander.src.Notifications.Interfaces;
using Lander.src.Notifications.Models;
using Lander.src.Notifications.NotificationsHub;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
namespace Lander.src.Notifications.Implementation;
public class NotificationService : INotificationService
{
    private readonly NotificationContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public NotificationService(NotificationContext context, IHubContext<NotificationHub> hubContext, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _hubContext = hubContext;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>How many unread notifications the bell loads when none is asked for, and the most it loads.</summary>
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 100;

    public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(int userId, int limit = DefaultPageSize)
    {
        // Somebody who never opens the bell gathers notifications for months: only the newest are sent.
        var rows = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedDate)
            .Take(Math.Clamp(limit, 1, MaxPageSize))
            .ToListAsync();
        return rows.Select(ToDto).ToList();
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
        var notification = await _context.Notifications
       .FirstOrDefaultAsync(n => n.Id == notificationId);
        if (notification != null)
        {
            await _context.RunInTransactionAsync(async () =>
            {
                await _context.ReadNotifications.AddAsync(ToRead(notification));
                _context.Notifications.Remove(notification);
                await _context.SaveEntitiesAsync();
            });
        }
    }

    public async Task<NotificationDto> SendNotificationAsync(CreateNotificationInputDto input)
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.Claims
         .FirstOrDefault(c => c.Type == "sub")?.Value;
        var callerGuid = Guid.TryParse(currentUserGuid, out var guid) ? guid : Guid.Empty;

        var notification = ToEntity(input, callerGuid);
        await _context.RunInTransactionAsync(async () =>
        {
            _context.Notifications.Add(notification);
            await _context.SaveEntitiesAsync();
        });

        // Push ONLY to the recipient's group. The previous Clients.All.SendAsync(dto) broadcast
        // the notification to every connected client (a privacy leak) AND sent a single object
        // where the client reads positional arguments, so it never rendered.
        //   1 title, 2 message (Serbian: the fallback), 3 type, 4 metadata (or the target),
        //   5 descriptor: the id (to mark it read), the translation key + values and the page to open.
        var descriptor = NotificationDescriptor.Create(input.Key, input.Params, notification.ActionTarget, notification.Id);
        await _hubContext.Clients
            .Group(notification.RecipientUserId.ToString())
            .SendAsync("ReceiveNotification",
                notification.Title,
                notification.Message,
                string.IsNullOrEmpty(input.Type) ? "info" : input.Type,
                input.Metadata ?? notification.ActionTarget ?? string.Empty,
                descriptor);

        return ToDto(notification);
    }

    public async Task<int> PersistNotificationAsync(CreateNotificationInputDto dto)
    {
        var notification = ToEntity(dto, dto.CreatedByGuid);
        await _context.RunInTransactionAsync(async () =>
        {
            _context.Notifications.Add(notification);
            await _context.SaveEntitiesAsync();
        });
        return notification.Id;
    }

    public async Task<bool> DeleteNotificationAsync(int notificationId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId);
        if (notification == null) return false;
        await _context.RunInTransactionAsync(async () =>
        {
            _context.Notifications.Remove(notification);
            await _context.SaveEntitiesAsync();
        });
        return true;
    }

    public async Task<bool> MarkAllAsReadAsync(int userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.RecipientUserId == userId && !n.IsRead)
            .ToListAsync();
        if (!notifications.Any()) return true;
        await _context.RunInTransactionAsync(async () =>
        {
            foreach (var notification in notifications)
                await _context.ReadNotifications.AddAsync(ToRead(notification));
            _context.Notifications.RemoveRange(notifications);
            await _context.SaveEntitiesAsync();
        });
        return true;
    }

    public async Task<NotificationDto?> GetNotificationByIdAsync(int notificationId)
    {
        var n = await _context.Notifications
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == notificationId);
        return n == null ? null : ToDto(n);
    }

    // ── mapping ──────────────────────────────────────────────────────────────

    private static Notification ToEntity(CreateNotificationInputDto input, Guid createdByGuid) => new()
    {
        Title = input.Title,
        Message = input.Message,
        ActionType = input.ActionType,
        ActionTarget = input.ActionTarget,
        Key = string.IsNullOrWhiteSpace(input.Key) ? null : input.Key,
        ParamsJson = input.Params is { Count: > 0 } ? JsonSerializer.Serialize(input.Params) : null,
        CreatedByGuid = createdByGuid,
        SenderUserId = input.SenderUserId,
        RecipientUserId = input.RecipientUserId,
        CreatedDate = DateTime.UtcNow,
    };

    private static ReadNotification ToRead(Notification n) => new()
    {
        // Id is omitted — auto-generated by the IDENTITY column
        Title = n.Title,
        Message = n.Message,
        ActionType = n.ActionType,
        ActionTarget = n.ActionTarget,
        Key = n.Key,
        ParamsJson = n.ParamsJson,
        IsRead = true,
        CreatedDate = n.CreatedDate,
        CreatedByGuid = n.CreatedByGuid,
        SenderUserId = n.SenderUserId,
        RecipientUserId = n.RecipientUserId,
    };

    private static NotificationDto ToDto(Notification n) => new()
    {
        Id = n.Id,
        Title = n.Title,
        Message = n.Message,
        ActionType = n.ActionType,
        ActionTarget = n.ActionTarget,
        Key = n.Key,
        Params = ParseParams(n.ParamsJson),
        IsRead = n.IsRead,
        CreatedDate = n.CreatedDate,
        CreatedByGuid = n.CreatedByGuid,
        SenderUserId = n.SenderUserId,
        RecipientUserId = n.RecipientUserId,
    };

    private static Dictionary<string, string>? ParseParams(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonSerializer.Deserialize<Dictionary<string, string>>(json); }
        catch (JsonException) { return null; }
    }
}
