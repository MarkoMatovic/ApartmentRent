using Lander.src.Common;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Notifications;

/// <summary>
/// Deletes all notifications addressed to a user when the account is deleted.
/// </summary>
public class NotificationUserDeletedHandler : IUserDeletedHandler
{
    private readonly NotificationContext _context;

    public NotificationUserDeletedHandler(NotificationContext context)
        => _context = context;

    public async Task HandleAsync(int userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.RecipientUserId == userId)
            .ToListAsync();

        if (notifications.Count > 0)
        {
            _context.Notifications.RemoveRange(notifications);
            await _context.SaveEntitiesAsync();
        }
    }
}
