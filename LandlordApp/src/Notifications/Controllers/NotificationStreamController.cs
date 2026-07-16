using Lander.src.Notifications.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace Lander.src.Notifications.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationStreamController : ControllerBase
{
    private readonly NotificationStreamService _streamService;

    public NotificationStreamController(NotificationStreamService streamService)
    {
        _streamService = streamService;
    }

    // SSE connections are long-lived; the global UseRequestTimeouts (default 30s) would
    // otherwise kill and churn every stream every 30 seconds. Opt this endpoint out.
    [HttpGet("stream")]
    [DisableRequestTimeout]
    public async Task StreamNotifications(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no");

        // Each SSE request gets a unique connectionId so two browser tabs of the
        // same user each have their own channel — TryRemove on one doesn't disrupt the other.
        var connectionId = Guid.NewGuid().ToString("N");

        try
        {
            await SendSseMessage("connected", new { userId, connectionId, timestamp = DateTime.UtcNow });

            await foreach (var notification in _streamService.StreamNotificationsAsync(userId, connectionId, cancellationToken))
            {
                await SendSseMessage("notification", notification);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    [HttpPost("test")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SendTestNotification([FromBody] string message)
    {
        var userId = GetCurrentUserId();

        var notification = new NotificationMessage(
            Type: "test",
            Title: "Test Notification",
            Message: message,
            Timestamp: DateTime.UtcNow
        );

        await _streamService.SendNotificationAsync(userId, notification);

        return Ok(new { success = true, message = "Notification sent" });
    }

    [HttpGet("connections")]
    public IActionResult GetConnectionCount()
    {
        var count = _streamService.GetActiveConnectionCount();
        return Ok(new { activeConnections = count });
    }

    private async Task SendSseMessage(string eventType, object data)
    {
        var json = JsonSerializer.Serialize(data);
        await Response.WriteAsync($"event: {eventType}\n");
        await Response.WriteAsync($"data: {json}\n\n");
        await Response.Body.FlushAsync();
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue("userId");
        return int.TryParse(claim, out var id) ? id : 0;
    }
}
