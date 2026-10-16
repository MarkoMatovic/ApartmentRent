using FluentAssertions;
using Lander.src.Modules.Communication.Hubs;
using Lander.src.Modules.Communication.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace LandlordApp.Tests.Hubs;

/// <summary>The bell's side of the chat hub (the notification hub was merged into it: one connection per tab).</summary>
public class NotificationHubTests
{
    private readonly Mock<IHubCallerClients> _mockClients;
    private readonly Mock<IClientProxy> _mockClientProxy;
    private readonly Mock<IGroupManager> _mockGroups;
    private readonly Mock<HubCallerContext> _mockContext;
    private readonly ChatHub _hub;

    public NotificationHubTests()
    {
        _mockClients     = new Mock<IHubCallerClients>();
        _mockClientProxy = new Mock<IClientProxy>();
        _mockGroups      = new Mock<IGroupManager>();
        _mockContext     = new Mock<HubCallerContext>();

        _mockContext.Setup(c => c.ConnectionId).Returns("conn-notify-1");

        // JoinNotificationGroup validates the "userId" claim against the argument
        var claims = new List<System.Security.Claims.Claim> { new("userId", "7") };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "Test");
        _mockContext.Setup(c => c.User).Returns(new System.Security.Claims.ClaimsPrincipal(identity));

        _mockClients.Setup(c => c.All).Returns(_mockClientProxy.Object);
        _mockGroups.Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cache = new MemoryDistributedCache(
            Microsoft.Extensions.Options.Options.Create(new MemoryDistributedCacheOptions()));
        _hub = new ChatHub(new Mock<IMessageService>().Object, cache)
        {
            Clients = _mockClients.Object,
            Groups  = _mockGroups.Object,
            Context = _mockContext.Object
        };
    }

    // ─── OnConnectedAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task OnConnectedAsync_PutsTheConnectionInTheOwnersNotificationGroup_WithoutTheClientAskingToJoin()
    {
        // The group of a connection is gone after an automatic reconnect; joining on connect keeps notifications flowing.
        await _hub.OnConnectedAsync();

        _mockGroups.Verify(
            g => g.AddToGroupAsync("conn-notify-1", "7", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnConnectedAsync_OneConnectionJoinsBothTheChatRoomAndTheNotificationGroup()
    {
        await _hub.OnConnectedAsync();

        _mockGroups.Verify(g => g.AddToGroupAsync("conn-notify-1", "user_7", It.IsAny<CancellationToken>()), Times.Once);
        _mockGroups.Verify(g => g.AddToGroupAsync("conn-notify-1", ChatHub.NotificationGroup(7), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnConnectedAsync_WithoutAUserClaim_JoinsNothing()
    {
        _mockContext.Setup(c => c.User).Returns(new System.Security.Claims.ClaimsPrincipal());

        await _hub.OnConnectedAsync();

        _mockGroups.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ─── JoinNotificationGroup ───────────────────────────────────────────────

    [Fact]
    public async Task JoinNotificationGroup_OwnUserId_AddsConnectionToGroup()
    {
        // Caller's "userId" claim is 7 — joining own group succeeds
        await _hub.JoinNotificationGroup(7);

        _mockGroups.Verify(
            g => g.AddToGroupAsync("conn-notify-1", "7", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task JoinNotificationGroup_DifferentUserId_AbortsConnection()
    {
        await _hub.JoinNotificationGroup(123);

        _mockContext.Verify(c => c.Abort(), Times.Once);
        _mockGroups.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task JoinNotificationGroup_NoUserClaim_AbortsConnection()
    {
        _mockContext.Setup(c => c.User).Returns(new System.Security.Claims.ClaimsPrincipal());

        await _hub.JoinNotificationGroup(7);

        _mockContext.Verify(c => c.Abort(), Times.Once);
        _mockGroups.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
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
