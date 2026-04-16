using FluentAssertions;
using Lander.src.Modules.Communication.Hubs;
using Lander.src.Modules.Communication.Interfaces;
using Lander.src.Modules.Communication.Dtos.Dto;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace LandlordApp.Tests.Hubs;

public class ChatHubTests
{
    private readonly Mock<IMessageService> _mockMessageService;
    private readonly Mock<IHubCallerClients> _mockClients;
    private readonly Mock<IClientProxy> _mockClientProxy;
    private readonly Mock<IGroupManager> _mockGroups;
    private readonly Mock<HubCallerContext> _mockContext;
    private readonly ChatHub _hub;

    public ChatHubTests()
    {
        _mockMessageService = new Mock<IMessageService>();
        _mockClients        = new Mock<IHubCallerClients>();
        _mockClientProxy    = new Mock<IClientProxy>();
        _mockGroups         = new Mock<IGroupManager>();
        _mockContext        = new Mock<HubCallerContext>();

        _mockContext.Setup(c => c.ConnectionId).Returns("conn-1");
        _mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);
        _mockGroups.Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockGroups.Setup(g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _hub = new ChatHub(_mockMessageService.Object)
        {
            Clients = _mockClients.Object,
            Groups  = _mockGroups.Object,
            Context = _mockContext.Object
        };
    }

    private static MessageDto MakeMessageDto(int senderId = 1, int receiverId = 2, string text = "Hi")
        => new MessageDto
        {
            MessageId         = 1,
            SenderId          = senderId,
            ReceiverId        = receiverId,
            MessageText       = text,
            SentAt            = DateTime.UtcNow,
            IsRead            = false,
            SenderName        = "Sender One",
            ReceiverName      = "Receiver One"
        };

    // ─── JoinChatRoom ────────────────────────────────────────────────────────

    [Fact]
    public async Task JoinChatRoom_AddsConnectionToCorrectGroup()
    {
        await _hub.JoinChatRoom(42);

        _mockGroups.Verify(
            g => g.AddToGroupAsync("conn-1", "user_42", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task JoinChatRoom_DifferentUserId_UsesCorrectGroupName()
    {
        await _hub.JoinChatRoom(99);

        _mockGroups.Verify(
            g => g.AddToGroupAsync("conn-1", "user_99", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ─── SendMessage ─────────────────────────────────────────────────────────

    [Fact]
    public async Task SendMessage_CallsMessageServiceAndBroadcastsToBothGroups()
    {
        var dto = MakeMessageDto(senderId: 1, receiverId: 2, text: "Hello");
        _mockMessageService.Setup(s => s.SendMessageAsync(1, 2, "Hello", false))
            .ReturnsAsync(dto);

        await _hub.SendMessage(1, 2, "Hello");

        _mockMessageService.Verify(s => s.SendMessageAsync(1, 2, "Hello", false), Times.Once);

        // ReceiveMessage to receiver group
        _mockClients.Verify(c => c.Group("user_2"), Times.AtLeastOnce);
        // MessageSent to sender group
        _mockClients.Verify(c => c.Group("user_1"), Times.AtLeastOnce);
    }

    [Fact]
    public async Task SendMessage_SendsReceiveMessageEventToReceiverGroup()
    {
        var dto = MakeMessageDto(senderId: 3, receiverId: 7);
        _mockMessageService.Setup(s => s.SendMessageAsync(3, 7, "Test", false))
            .ReturnsAsync(dto);

        await _hub.SendMessage(3, 7, "Test");

        _mockClientProxy.Verify(
            c => c.SendCoreAsync("ReceiveMessage", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendMessage_SendsMessageSentEventToSenderGroup()
    {
        var dto = MakeMessageDto(senderId: 3, receiverId: 7);
        _mockMessageService.Setup(s => s.SendMessageAsync(3, 7, "Test", false))
            .ReturnsAsync(dto);

        await _hub.SendMessage(3, 7, "Test");

        _mockClientProxy.Verify(
            c => c.SendCoreAsync("MessageSent", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ─── MarkMessageAsRead ───────────────────────────────────────────────────

    [Fact]
    public async Task MarkMessageAsRead_CallsMarkAsReadOnService()
    {
        var dto = MakeMessageDto(senderId: 1, receiverId: 2);
        _mockMessageService.Setup(s => s.MarkAsReadAsync(10)).Returns(Task.CompletedTask);
        _mockMessageService.Setup(s => s.GetMessageByIdAsync(10)).ReturnsAsync(dto);

        await _hub.MarkMessageAsRead(10);

        _mockMessageService.Verify(s => s.MarkAsReadAsync(10), Times.Once);
    }

    [Fact]
    public async Task MarkMessageAsRead_MessageFound_BroadcastsMessageReadEvent()
    {
        var dto = MakeMessageDto(senderId: 5, receiverId: 2);
        _mockMessageService.Setup(s => s.MarkAsReadAsync(10)).Returns(Task.CompletedTask);
        _mockMessageService.Setup(s => s.GetMessageByIdAsync(10)).ReturnsAsync(dto);

        await _hub.MarkMessageAsRead(10);

        _mockClients.Verify(c => c.Group("user_5"), Times.Once);
        _mockClientProxy.Verify(
            c => c.SendCoreAsync("MessageRead", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MarkMessageAsRead_MessageNotFound_DoesNotBroadcast()
    {
        _mockMessageService.Setup(s => s.MarkAsReadAsync(999)).Returns(Task.CompletedTask);
        _mockMessageService.Setup(s => s.GetMessageByIdAsync(999)).ReturnsAsync((MessageDto?)null);

        await _hub.MarkMessageAsRead(999);

        _mockClientProxy.Verify(
            c => c.SendCoreAsync("MessageRead", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ─── UserTyping ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UserTyping_SendsUserTypingEventToReceiverGroup()
    {
        await _hub.UserTyping(1, 3);

        _mockClients.Verify(c => c.Group("user_3"), Times.Once);
        _mockClientProxy.Verify(
            c => c.SendCoreAsync("UserTyping", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UserTyping_DoesNotSendToSenderGroup()
    {
        await _hub.UserTyping(1, 3);

        // Verify group "user_1" was never requested
        _mockClients.Verify(c => c.Group("user_1"), Times.Never);
    }

    // ─── OnConnectedAsync / OnDisconnectedAsync ───────────────────────────────

    [Fact]
    public async Task OnConnectedAsync_DoesNotThrow()
    {
        var act = async () => await _hub.OnConnectedAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task OnDisconnectedAsync_DoesNotThrow()
    {
        var act = async () => await _hub.OnDisconnectedAsync(null);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task OnDisconnectedAsync_WithException_DoesNotThrow()
    {
        var act = async () => await _hub.OnDisconnectedAsync(new Exception("connection dropped"));
        await act.Should().NotThrowAsync();
    }
}
