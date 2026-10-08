using Lander.src.Notifications.Dtos.Dto;
using Lander.src.Notifications.Dtos.InputDto;
namespace Lander.src.Notifications.Interfaces;
public interface INotificationService
{
    Task<NotificationDto> SendNotificationAsync(CreateNotificationInputDto createNotificationInputDto);

    /// <summary>
    /// Stores a notification in the bell history WITHOUT pushing over SignalR. Used by flows
    /// that already push their own real-time message (chat, applications) but also want the
    /// notification to persist so it survives offline and shows up in the bell later.
    /// </summary>
    Task PersistNotificationAsync(CreateNotificationInputDto createNotificationInputDto);

    Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(int userId);
    Task MarkAsReadAsync(int notificationId);
    Task<bool> DeleteNotificationAsync(int notificationId);
    Task<bool> MarkAllAsReadAsync(int userId);
    Task<NotificationDto?> GetNotificationByIdAsync(int notificationId);
}
