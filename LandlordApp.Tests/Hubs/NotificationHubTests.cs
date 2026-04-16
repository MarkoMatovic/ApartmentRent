using FluentAssertions;
using Lander.src.Notifications.NotificationsHub;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace LandlordApp.Tests.Hubs;

public class NotificationHubTests
{
    private readonly Mock<IHubCallerClients> _mockClients;
    private readonly Mock<IClientProxy> _mockClientProxy;
    private readonly Mock<IGroupManager> _mockGroups;
    private readonly Mock<HubCallerContext> _mockContext;
    private readonly NotificationHub _hub;

    public NotificationHubTests()
    {
        _mockClients     = new Mock<IHubCallerClients>();
        _mockClientProxy = new Mock<IClientProxy>();
        _mockGroups      = new Mock<IGroupManager>();
        _mockContext     = new Mock<HubCallerContext>();

        _mockContext.Setup(c => c.ConnectionId).Returns("conn-notify-1");
        _mockClients.Setup(c => c.All).Returns(_mockClientProxy.Object);
        _mockGroups.Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _hub = new NotificationHub
        {
            Clients = _mockClients.Object,
            Groups  = _mockGroups.Object,
            Context = _mockContext.Object
        };
    }

    // ─── JoinNotificationGroup ───────────────────────────────────────────────

    [Fact]
    public async Task JoinNotificationGroup_AddsConnectionToGroupWithUserId()
    {
        await _hub.JoinNotificationGroup(7);

        _mockGroups.Verify(
            g => g.AddToGroupAsync("conn-notify-1", "7", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task JoinNotificationGroup_DifferentUserId_UsesStringRepresentation()
    {
        await _hub.JoinNotificationGroup(123);

        _mockGroups.Verify(
            g => g.AddToGroupAsync("conn-notify-1", "123", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task JoinNotificationGroup_ZeroUserId_UsesZeroAsGroupName()
    {
        await _hub.JoinNotificationGroup(0);

        _mockGroups.Verify(
            g => g.AddToGroupAsync("conn-notify-1", "0", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ─── SendNotificationToAll ───────────────────────────────────────────────

    [Fact]
    public async Task SendNotificationToAll_BroadcastsReceiveNotificationEvent()
    {
        await _hub.SendNotificationToAll("System maintenance at 3am");

        _mockClientProxy.Verify(
            c => c.SendCoreAsync(
                "ReceiveNotification",
                It.Is<object[]>(args => args.Length == 1 && (string)args[0] == "System maintenance at 3am"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendNotificationToAll_EmptyMessage_StillBroadcasts()
    {
        await _hub.SendNotificationToAll("");

        _mockClientProxy.Verify(
            c => c.SendCoreAsync("ReceiveNotification", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendNotificationToAll_UsesClientsAll()
    {
        await _hub.SendNotificationToAll("hello");

        _mockClients.Verify(c => c.All, Times.Once);
    }
}
