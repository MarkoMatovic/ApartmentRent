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
    Task<int> PersistNotificationAsync(CreateNotificationInputDto createNotificationInputDto);

    /// <summary>The newest unread notifications of the user, at most <paramref name="limit"/> (up to 100).</summary>
    Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(int userId, int limit = 50);
    Task MarkAsReadAsync(int notificationId);
    Task<bool> DeleteNotificationAsync(int notificationId);
    Task<bool> MarkAllAsReadAsync(int userId);
    Task<NotificationDto?> GetNotificationByIdAsync(int notificationId);
}
