using Lander.src.Notifications.Services;
using Microsoft.AspNetCore.Authorization;
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
    
    /// <summary>
    /// .NET 10 Feature: Server-Sent Events endpoint for real-time notifications
    /// </summary>
    [HttpGet("stream")]
    public async Task StreamNotifications(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        
        // Configure SSE headers
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no"); // Disable nginx buffering
        
        try
        {
            // Send initial connection confirmation
            await SendSseMessage("connected", new { userId, timestamp = DateTime.UtcNow });
            
            // Stream notifications
            await foreach (var notification in _streamService.StreamNotificationsAsync(userId, cancellationToken))
            {
                await SendSseMessage("notification", notification);
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected - this is normal
        }
    }
    
    /// <summary>
    /// Test endpoint to send a notification to current user
    /// </summary>
    [HttpPost("test")]
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
    
    /// <summary>
    /// Get active SSE connection count (admin only)
    /// </summary>
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
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }
}
