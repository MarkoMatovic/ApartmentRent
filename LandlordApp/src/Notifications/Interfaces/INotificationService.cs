using Lander.src.Notifications.Dtos.Dto;
using Lander.src.Notifications.Dtos.InputDto;

namespace Lander.src.Notifications.Interfaces;

public interface INotificationService
{
    Task<NotificationDto> SendNotificationAsync(CreateNotificationInputDto createNotificationInputDto);
    Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(int userId);
    Task MarkAsReadAsync(int notificationId);
}
