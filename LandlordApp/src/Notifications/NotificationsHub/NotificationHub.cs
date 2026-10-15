using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Lander.src.Notifications.NotificationsHub;

[Authorize]
public class NotificationHub : Hub
{
    /// <summary>
    /// Every connection is put into its owner's group as soon as it opens (the user id comes from the token).
    /// Groups belong to a connection, so after an automatic reconnect the client would otherwise get nothing
    /// until it asked to join again.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        if (int.TryParse(Context.User?.FindFirstValue("userId"), out var userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, userId.ToString());

        await base.OnConnectedAsync();
    }

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
