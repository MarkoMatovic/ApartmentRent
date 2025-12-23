using System;
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


    public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(int userId)
    {
        return await _context.Notifications
            .Where(n => n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedDate)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                ActionType = n.ActionType,
                ActionTarget = n.ActionTarget,
                IsRead = n.IsRead,
                CreatedDate = n.CreatedDate,
                CreatedByGuid = n.CreatedByGuid,
                SenderUserId = n.SenderUserId,
                RecipientUserId = n.RecipientUserId
            }).ToListAsync();
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
        var notification = await _context.Notifications
       .FirstOrDefaultAsync(n => n.Id == notificationId);

        if (notification != null)
        {
            var readNotification = new ReadNotification
            {
                Id = notification.Id,
                Title = notification.Title,
                Message = notification.Message,
                ActionType = notification.ActionType,
                ActionTarget = notification.ActionTarget,
                IsRead = true, 
                CreatedDate = notification.CreatedDate,
                CreatedByGuid = notification.CreatedByGuid,
                SenderUserId = notification.SenderUserId,
                RecipientUserId = notification.RecipientUserId
            };
            var transaction = await _context.BeginTransactionAsync();
            try
            {
                await _context.ReadNotifications.AddAsync(readNotification);
                _context.Notifications.Remove(notification);
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

    public async Task<NotificationDto> SendNotificationAsync(CreateNotificationInputDto createNotificationInputDto)
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.Claims
         .FirstOrDefault(c => c.Type == "sub")?.Value;

        var notification = new Notification
        {
            Title = createNotificationInputDto.Title,
            Message = createNotificationInputDto.Message,
            ActionType = createNotificationInputDto.ActionType,
            ActionTarget = createNotificationInputDto.ActionTarget,
            CreatedByGuid = Guid.Parse(currentUserGuid),
            SenderUserId = createNotificationInputDto.SenderUserId,
            RecipientUserId = createNotificationInputDto.RecipientUserId,
            CreatedDate = DateTime.UtcNow,
        };

        var transaction = await _context.BeginTransactionAsync();
        try
        {
            _context.Notifications.Add(notification);
            await _context.SaveEntitiesAsync();
            await _context.CommitTransactionAsync(transaction);
        }
        catch
        {
            _context.RollBackTransaction();
            throw;
        }

        
        var notificationDto = new NotificationDto
        {
            Id = notification.Id,
            Title = notification.Title,
            Message = notification.Message,
            ActionType = notification.ActionType,
            ActionTarget = notification.ActionTarget,
            IsRead = notification.IsRead
        };

        await _hubContext.Clients.All.SendAsync("ReceiveNotification", notificationDto);
        return notificationDto;

    }

}

