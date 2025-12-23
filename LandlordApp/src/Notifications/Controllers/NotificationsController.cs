using Lander.Helpers;
using Lander.src.Notifications.Dtos.Dto;
using Lander.src.Notifications.Dtos.InputDto;
using Lander.src.Notifications.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lander.src.Notifications.Controllers
{
    [Route(ApiActionsV1.Notification)]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        #region Properties
        private readonly INotificationService _notificationService;
        #endregion
        #region Constructors
        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }
        #endregion

        [HttpGet(ApiActionsV1.GetUserNotifications, Name = nameof(ApiActionsV1.GetUserNotifications))]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetUserNotifications([FromQuery] int id)
        {
            return Ok(await _notificationService.GetUserNotificationsAsync(id));
        }
        [HttpPost(ApiActionsV1.SendNotification, Name = nameof(ApiActionsV1.SendNotification))]
        public async Task<ActionResult<NotificationDto>> SendNotification([FromBody] CreateNotificationInputDto createNotificationInputDto)
        {
            return Ok(await _notificationService.SendNotificationAsync(createNotificationInputDto));
        }

        [HttpPost(ApiActionsV1.MarkAsRead, Name = nameof(ApiActionsV1.MarkAsRead))]
        public async Task<IActionResult> MarkRead(int notificationId)
        {
            await _notificationService.MarkAsReadAsync(notificationId);
            return Ok();
        }


    }
}
