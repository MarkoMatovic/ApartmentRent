using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Lander.src.Notifications.NotificationsHub;

[Authorize]
public class NotificationHub : Hub
{
    /// <summary>
    /// Joins the caller to their own notification group.
    /// The userId is taken from the JWT claim — the client-supplied parameter
    /// is ignored to prevent a user from subscribing to another user's stream.
    /// </summary>
    public async Task JoinNotificationGroup(int userId)
    {
        var callerIdClaim = Context.User?.FindFirstValue("userId");
        if (!int.TryParse(callerIdClaim, out var callerId))
        {
            Context.Abort();
            return;
        }

        // Reject if the client tries to subscribe to a different user's group
        if (callerId != userId)
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, callerId.ToString());
    }

    /// <summary>Admin-only broadcast — restricted to Admin role.</summary>
    [Authorize(Roles = "Admin")]
    public async Task SendNotificationToAll(string message)
    {
        await Clients.All.SendAsync("ReceiveNotification", message);
    }
}
