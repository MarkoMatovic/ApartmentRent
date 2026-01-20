using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;
namespace Lander.src.Notifications.NotificationsHub;
public class NotificationHub : Hub
{
    public async Task JoinNotificationGroup(int userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, userId.ToString());
    }
    public async Task SendNotificationToAll(string message)
    {
        await Clients.All.SendAsync("ReceiveNotification", message);
    }
}
