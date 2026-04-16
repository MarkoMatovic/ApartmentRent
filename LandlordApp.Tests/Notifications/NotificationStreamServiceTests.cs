using FluentAssertions;
using Lander.src.Notifications.Services;

namespace LandlordApp.Tests.Notifications;

public class NotificationStreamServiceTests
{
    private static NotificationMessage MakeMessage(string type = "info", string title = "Title", string message = "Message")
        => new(type, title, message, DateTime.UtcNow);

    // ─── GetActiveConnectionCount ────────────────────────────────────────────

    [Fact]
    public void GetActiveConnectionCount_NoConnections_ReturnsZero()
    {
        var service = new NotificationStreamService();

        service.GetActiveConnectionCount().Should().Be(0);
    }

    [Fact]
    public async Task GetActiveConnectionCount_AfterStreamStarted_ReturnsOne()
    {
        var service = new NotificationStreamService();
        using var cts = new CancellationTokenSource();

        // Start streaming (creates the channel) — run in background so we don't block
        var streamTask = Task.Run(async () =>
        {
            await foreach (var _ in service.StreamNotificationsAsync(1, cts.Token)) { }
        });

        // Give time for the channel to be registered
        await Task.Delay(50);

        service.GetActiveConnectionCount().Should().Be(1);

        cts.Cancel();
        await streamTask.ContinueWith(_ => { }); // swallow cancellation
    }

    [Fact]
    public async Task GetActiveConnectionCount_AfterStreamCancelled_ReturnsZero()
    {
        var service = new NotificationStreamService();
        using var cts = new CancellationTokenSource();

        var streamTask = Task.Run(async () =>
        {
            await foreach (var _ in service.StreamNotificationsAsync(1, cts.Token)) { }
        });

        await Task.Delay(50);

        cts.Cancel();
        await streamTask.ContinueWith(_ => { });

        // Channel is removed in the finally block of StreamNotificationsAsync
        await Task.Delay(50);
        service.GetActiveConnectionCount().Should().Be(0);
    }

    // ─── SendNotificationAsync ───────────────────────────────────────────────

    [Fact]
    public async Task SendNotificationAsync_NoActiveStream_DoesNotThrow()
    {
        var service = new NotificationStreamService();
        var msg = MakeMessage();

        var act = async () => await service.SendNotificationAsync(42, msg);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendNotificationAsync_WithActiveStream_MessageIsReceived()
    {
        var service = new NotificationStreamService();
        using var cts = new CancellationTokenSource();
        var received = new List<NotificationMessage>();

        var streamTask = Task.Run(async () =>
        {
            await foreach (var n in service.StreamNotificationsAsync(1, cts.Token))
            {
                received.Add(n);
                cts.Cancel(); // cancel after first message
            }
        });

        await Task.Delay(50); // let the stream register

        var msg = MakeMessage("test", "Hello", "World");
        await service.SendNotificationAsync(1, msg);

        await streamTask.ContinueWith(_ => { });

        received.Should().ContainSingle();
        received[0].Type.Should().Be("test");
        received[0].Title.Should().Be("Hello");
        received[0].Message.Should().Be("World");
    }

    [Fact]
    public async Task SendNotificationAsync_WrongUserId_MessageNotDelivered()
    {
        var service = new NotificationStreamService();
        using var cts = new CancellationTokenSource();
        var received = new List<NotificationMessage>();

        var streamTask = Task.Run(async () =>
        {
            await foreach (var n in service.StreamNotificationsAsync(userId: 1, cts.Token))
                received.Add(n);
        });

        await Task.Delay(50);

        // Send to userId=2 — user 1 should not receive it
        await service.SendNotificationAsync(2, MakeMessage());
        await Task.Delay(50);

        cts.Cancel();
        await streamTask.ContinueWith(_ => { });

        received.Should().BeEmpty();
    }

    // ─── BroadcastNotificationAsync ──────────────────────────────────────────

    [Fact]
    public async Task BroadcastNotificationAsync_NoStreams_DoesNotThrow()
    {
        var service = new NotificationStreamService();
        var msg = MakeMessage();

        var act = async () => await service.BroadcastNotificationAsync(msg);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task BroadcastNotificationAsync_MultipleStreams_AllReceiveMessage()
    {
        var service = new NotificationStreamService();
        using var cts1 = new CancellationTokenSource();
        using var cts2 = new CancellationTokenSource();

        var received1 = new List<NotificationMessage>();
        var received2 = new List<NotificationMessage>();

        var task1 = Task.Run(async () =>
        {
            await foreach (var n in service.StreamNotificationsAsync(1, cts1.Token))
            {
                received1.Add(n);
                cts1.Cancel();
            }
        });

        var task2 = Task.Run(async () =>
        {
            await foreach (var n in service.StreamNotificationsAsync(2, cts2.Token))
            {
                received2.Add(n);
                cts2.Cancel();
            }
        });

        await Task.Delay(80); // let both streams register

        var broadcastMsg = MakeMessage("broadcast", "Broadcast", "Hello all");
        await service.BroadcastNotificationAsync(broadcastMsg);

        await Task.WhenAll(
            task1.ContinueWith(_ => { }),
            task2.ContinueWith(_ => { }));

        received1.Should().ContainSingle().Which.Type.Should().Be("broadcast");
        received2.Should().ContainSingle().Which.Type.Should().Be("broadcast");
    }

    // ─── NotificationMessage record ──────────────────────────────────────────

    [Fact]
    public void NotificationMessage_OptionalFields_DefaultToNull()
    {
        var msg = new NotificationMessage("info", "Title", "Body", DateTime.UtcNow);

        msg.ActionUrl.Should().BeNull();
        msg.Data.Should().BeNull();
    }

    [Fact]
    public void NotificationMessage_WithAllFields_SetsCorrectly()
    {
        var ts = DateTime.UtcNow;
        var msg = new NotificationMessage("alert", "T", "M", ts, "/action", new { id = 1 });

        msg.Type.Should().Be("alert");
        msg.Title.Should().Be("T");
        msg.Message.Should().Be("M");
        msg.Timestamp.Should().Be(ts);
        msg.ActionUrl.Should().Be("/action");
        msg.Data.Should().NotBeNull();
    }
}
