using Lander.Helpers;
using Lander.src.Common;
using Lander.src.Notifications.Dtos.Dto;
using Lander.src.Notifications.Dtos.InputDto;
using Lander.src.Notifications.Interfaces;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lander.src.Notifications.Controllers;

[Route(ApiActionsV1.Notification)]
[ApiController]
[Authorize]
public class NotificationsController : ApiControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService, IUserInterface userService)
        : base(userService)
    {
        _notificationService = notificationService;
    }

    [HttpGet(ApiActionsV1.GetUserNotifications, Name = nameof(ApiActionsV1.GetUserNotifications))]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> GetUserNotifications([FromQuery] int id)
    {
        var callerId = TryGetCurrentUserId();
        if (callerId is null) return Unauthorized();
        if (callerId.Value != id) return Forbid();

        return Ok(await _notificationService.GetUserNotificationsAsync(id));
    }

   
    [HttpPost(ApiActionsV1.SendNotification, Name = nameof(ApiActionsV1.SendNotification))]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<NotificationDto>> SendNotification([FromBody] CreateNotificationInputDto createNotificationInputDto)
    {
        return Ok(await _notificationService.SendNotificationAsync(createNotificationInputDto));
    }

    [HttpPost(ApiActionsV1.MarkAsRead, Name = nameof(ApiActionsV1.MarkAsRead))]
    public async Task<IActionResult> MarkRead([FromQuery] int notificationId)
    {
        var callerId = TryGetCurrentUserId();
        if (callerId is null) return Unauthorized();

        var notification = await _notificationService.GetNotificationByIdAsync(notificationId);
        if (notification == null) return NotFound();
        if (notification.RecipientUserId != callerId.Value) return Forbid();

        await _notificationService.MarkAsReadAsync(notificationId);
        return Ok();
    }

    [HttpDelete(ApiActionsV1.DeleteNotification, Name = nameof(ApiActionsV1.DeleteNotification))]
    public async Task<ActionResult<bool>> DeleteNotification([FromRoute] int id)
    {
        var callerId = TryGetCurrentUserId();
        if (callerId is null) return Unauthorized();

        var notification = await _notificationService.GetNotificationByIdAsync(id);
        if (notification == null) return NotFound();
        if (notification.RecipientUserId != callerId.Value) return Forbid();

        var result = await _notificationService.DeleteNotificationAsync(id);
        return Ok(result);
    }

    [HttpPost(ApiActionsV1.MarkAllAsRead, Name = nameof(ApiActionsV1.MarkAllAsRead))]
    public async Task<ActionResult<bool>> MarkAllAsRead()
    {
        var callerId = TryGetCurrentUserId();
        if (callerId is null) return Unauthorized();

        var result = await _notificationService.MarkAllAsReadAsync(callerId.Value);
        return Ok(result);
    }
}
