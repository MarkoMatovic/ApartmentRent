using FluentAssertions;
using Lander.src.Notifications.Controllers;
using Lander.src.Notifications.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace LandlordApp.Tests.Notifications;

public class NotificationStreamControllerTests
{
    private static NotificationStreamController CreateController(
        NotificationStreamService? service = null,
        int userId = 1)
    {
        service ??= new NotificationStreamService();

        var controller = new NotificationStreamController(service);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        // Provide a writable response body so SSE writes don't throw
        httpContext.Response.Body = new MemoryStream();

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    // ─── POST /api/notifications/test ────────────────────────────────────────

    [Fact]
    public async Task SendTestNotification_ValidMessage_ReturnsOkWithSuccess()
    {
        var controller = CreateController();

        var result = await controller.SendTestNotification("Hello!");

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().NotBeNull();
        // Anonymous object ToString: "{ success = True, message = Notification sent }"
        ok.Value!.ToString().Should().Contain("Notification sent");
    }

    [Fact]
    public async Task SendTestNotification_NoActiveStream_DoesNotThrowAndReturnsOk()
    {
        // No stream open — SendNotificationAsync is a no-op when channel absent
        var controller = CreateController();

        var act = async () => await controller.SendTestNotification("test message");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendTestNotification_DeliversToActiveStream()
    {
        var service = new NotificationStreamService();
        using var cts = new CancellationTokenSource();
        var received = new List<NotificationMessage>();

        // Open a stream for userId=5
        var streamTask = Task.Run(async () =>
        {
            await foreach (var n in service.StreamNotificationsAsync(5, cts.Token))
            {
                received.Add(n);
                cts.Cancel();
            }
        });

        await Task.Delay(50);

        var controller = CreateController(service, userId: 5);
        await controller.SendTestNotification("ping");

        await streamTask.ContinueWith(_ => { });

        received.Should().ContainSingle();
        received[0].Type.Should().Be("test");
        received[0].Message.Should().Be("ping");
    }

    // ─── GET /api/notifications/connections ──────────────────────────────────

    [Fact]
    public void GetConnectionCount_NoStreams_ReturnsZero()
    {
        var controller = CreateController();

        var result = controller.GetConnectionCount();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().NotBeNull();
        ok.Value!.ToString().Should().Contain("0");
    }

    [Fact]
    public async Task GetConnectionCount_WithActiveStream_ReturnsOne()
    {
        var service = new NotificationStreamService();
        using var cts = new CancellationTokenSource();

        var streamTask = Task.Run(async () =>
        {
            await foreach (var _ in service.StreamNotificationsAsync(1, cts.Token)) { }
        });

        await Task.Delay(50);

        var controller = CreateController(service, userId: 1);
        var result = controller.GetConnectionCount();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value!.ToString().Should().Contain("1");

        cts.Cancel();
        await streamTask.ContinueWith(_ => { });
    }

    // ─── GET /api/notifications/stream ───────────────────────────────────────

    [Fact]
    public async Task StreamNotifications_CancelledImmediately_CompletesWithoutException()
    {
        var service = new NotificationStreamService();
        var controller = CreateController(service, userId: 1);

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // cancel before even starting

        // Should not throw — OperationCanceledException is caught internally
        var act = async () => await controller.StreamNotifications(cts.Token);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StreamNotifications_SetsCorrectResponseHeaders()
    {
        var service = new NotificationStreamService();
        var controller = CreateController(service, userId: 1);

        using var cts = new CancellationTokenSource();

        // Cancel quickly to avoid blocking forever
        var streamTask = controller.StreamNotifications(cts.Token);
        await Task.Delay(30);
        cts.Cancel();
        await streamTask.ContinueWith(_ => { });

        var headers = controller.HttpContext.Response.Headers;
        headers["Content-Type"].ToString().Should().Contain("text/event-stream");
        headers["Cache-Control"].ToString().Should().Contain("no-cache");
    }

    // ─── GetCurrentUserId (via SendTestNotification) ─────────────────────────

    [Fact]
    public async Task SendTestNotification_NoUserClaim_UsesZeroAsId()
    {
        var service = new NotificationStreamService();
        var controller = new NotificationStreamController(service);

        // No claims — GetCurrentUserId returns 0
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Should not throw
        var act = async () => await controller.SendTestNotification("msg");
        await act.Should().NotThrowAsync();
    }
}
