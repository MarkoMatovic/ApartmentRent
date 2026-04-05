using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Lander.src.Modules.Analytics.Interfaces;
using Lander.src.Modules.Communication.Controllers;
using Lander.src.Modules.Communication.Dtos.Dto;
using Lander.src.Modules.Communication.Dtos.InputDto;
using Lander.src.Modules.Communication.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Lander.Helpers;

namespace LandlordApp.Tests.Controllers;

public class MessagesControllerTests
{
    private readonly Mock<IMessageService> _mockMessageService;
    private readonly Mock<IAnalyticsService> _mockAnalytics;
    private readonly MessagesController _controller;
    private const int CurrentUserId = 10;

    public MessagesControllerTests()
    {
        _mockMessageService = new Mock<IMessageService>();
        _mockAnalytics = new Mock<IAnalyticsService>();

        _mockAnalytics.Setup(a => a.TrackEventAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<Dictionary<string, string>?>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        _controller = new MessagesController(_mockMessageService.Object, _mockAnalytics.Object,
            new IdempotencyService(new MemoryCache(new MemoryCacheOptions())));
        _controller.ControllerContext = MakeAuthContext(CurrentUserId);
    }

    // ─── GetConversation ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetConversation_AsUserId1_ReturnsOk()
    {
        _mockMessageService.Setup(s => s.GetConversationAsync(CurrentUserId, 20, 1, 50))
            .ReturnsAsync(new ConversationMessagesDto());

        var result = await _controller.GetConversation(CurrentUserId, 20);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetConversation_AsUserId2_ReturnsOk()
    {
        _mockMessageService.Setup(s => s.GetConversationAsync(5, CurrentUserId, 1, 50))
            .ReturnsAsync(new ConversationMessagesDto());

        var result = await _controller.GetConversation(5, CurrentUserId);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetConversation_AsNonParticipant_ReturnsForbid()
    {
        var result = await _controller.GetConversation(1, 2); // currentUserId=10 not in {1,2}

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetConversation_PageSizeExceeds100_Capped()
    {
        _mockMessageService.Setup(s => s.GetConversationAsync(CurrentUserId, 20, 1, 100))
            .ReturnsAsync(new ConversationMessagesDto());

        var result = await _controller.GetConversation(CurrentUserId, 20, 1, 999);

        result.Result.Should().BeOfType<OkObjectResult>();
        _mockMessageService.Verify(s => s.GetConversationAsync(CurrentUserId, 20, 1, 100), Times.Once);
    }

    // ─── GetUserConversations ─────────────────────────────────────────────────

    [Fact]
    public async Task GetUserConversations_AsOwner_ReturnsOk()
    {
        _mockMessageService.Setup(s => s.GetUserConversationsAsync(CurrentUserId))
            .ReturnsAsync(new List<ConversationDto>());

        var result = await _controller.GetUserConversations(CurrentUserId);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetUserConversations_AsOther_ReturnsForbid()
    {
        var result = await _controller.GetUserConversations(99);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    // ─── SendMessage ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SendMessage_AsOwner_ReturnsOk()
    {
        var input = new SendMessageInputDto { SenderId = CurrentUserId, ReceiverId = 20, MessageText = "Hello" };
        _mockMessageService.Setup(s => s.SendMessageAsync(CurrentUserId, 20, "Hello", false))
            .ReturnsAsync(new MessageDto());

        var result = await _controller.SendMessage(input);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SendMessage_AsDifferentUser_ReturnsForbid()
    {
        var input = new SendMessageInputDto { SenderId = 99, ReceiverId = 20, MessageText = "Hi" };

        var result = await _controller.SendMessage(input);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    // ─── MarkAsRead ───────────────────────────────────────────────────────────

    [Fact]
    public async Task MarkAsRead_ReturnsOk()
    {
        _mockMessageService.Setup(s => s.MarkAsReadAsync(5)).Returns(Task.CompletedTask);

        var result = await _controller.MarkAsRead(5);

        result.Should().BeOfType<OkResult>();
    }

    // ─── GetUnreadCount ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetUnreadCount_AsOwner_ReturnsCount()
    {
        _mockMessageService.Setup(s => s.GetUnreadCountAsync(CurrentUserId)).ReturnsAsync(3);

        var result = await _controller.GetUnreadCount(CurrentUserId);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(3);
    }

    [Fact]
    public async Task GetUnreadCount_AsOther_ReturnsForbid()
    {
        var result = await _controller.GetUnreadCount(99);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    // ─── Archive / Unarchive ──────────────────────────────────────────────────

    [Fact]
    public async Task ArchiveConversation_AsOwner_ReturnsOk()
    {
        var dto = new ChatActionDto { UserId = CurrentUserId, OtherUserId = 20 };
        _mockMessageService.Setup(s => s.ArchiveConversationAsync(CurrentUserId, 20)).Returns(Task.CompletedTask);

        var result = await _controller.ArchiveConversation(dto);

        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task ArchiveConversation_AsOther_ReturnsForbid()
    {
        var result = await _controller.ArchiveConversation(new ChatActionDto { UserId = 99, OtherUserId = 20 });
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task UnarchiveConversation_AsOwner_ReturnsOk()
    {
        var dto = new ChatActionDto { UserId = CurrentUserId, OtherUserId = 20 };
        _mockMessageService.Setup(s => s.UnarchiveConversationAsync(CurrentUserId, 20)).Returns(Task.CompletedTask);

        var result = await _controller.UnarchiveConversation(dto);
        result.Should().BeOfType<OkResult>();
    }

    // ─── Mute / Unmute ────────────────────────────────────────────────────────

    [Fact]
    public async Task MuteConversation_AsOwner_ReturnsOk()
    {
        var dto = new ChatActionDto { UserId = CurrentUserId, OtherUserId = 20 };
        _mockMessageService.Setup(s => s.MuteConversationAsync(CurrentUserId, 20)).Returns(Task.CompletedTask);

        var result = await _controller.MuteConversation(dto);
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task UnmuteConversation_AsOwner_ReturnsOk()
    {
        var dto = new ChatActionDto { UserId = CurrentUserId, OtherUserId = 20 };
        _mockMessageService.Setup(s => s.UnmuteConversationAsync(CurrentUserId, 20)).Returns(Task.CompletedTask);

        var result = await _controller.UnmuteConversation(dto);
        result.Should().BeOfType<OkResult>();
    }

    // ─── Block / Unblock ──────────────────────────────────────────────────────

    [Fact]
    public async Task BlockUser_AsOwner_ReturnsOk()
    {
        var dto = new ChatActionDto { UserId = CurrentUserId, OtherUserId = 20 };
        _mockMessageService.Setup(s => s.BlockUserAsync(CurrentUserId, 20)).Returns(Task.CompletedTask);

        var result = await _controller.BlockUser(dto);
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task BlockUser_AsOther_ReturnsForbid()
    {
        var result = await _controller.BlockUser(new ChatActionDto { UserId = 99, OtherUserId = 20 });
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task UnblockUser_AsOwner_ReturnsOk()
    {
        var dto = new ChatActionDto { UserId = CurrentUserId, OtherUserId = 20 };
        _mockMessageService.Setup(s => s.UnblockUserAsync(CurrentUserId, 20)).Returns(Task.CompletedTask);

        var result = await _controller.UnblockUser(dto);
        result.Should().BeOfType<OkResult>();
    }

    // ─── DeleteConversation ───────────────────────────────────────────────────

    [Fact]
    public async Task DeleteConversation_ReturnsOk()
    {
        _mockMessageService.Setup(s => s.DeleteConversationAsync(CurrentUserId, 20)).Returns(Task.CompletedTask);

        var result = await _controller.DeleteConversation(20);
        result.Should().BeOfType<OkResult>();
    }

    // ─── SearchMessages ───────────────────────────────────────────────────────

    [Fact]
    public async Task SearchMessages_ReturnsOk()
    {
        _mockMessageService.Setup(s => s.SearchMessagesAsync(CurrentUserId, "hello"))
            .ReturnsAsync(new List<MessageDto>());

        var result = await _controller.SearchMessages("hello");

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    // ─── ReportAbuse ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ReportAbuse_AsOwner_ReturnsOk()
    {
        var dto = new ReportAbuseRequestDto { UserId = CurrentUserId, ReportedUserId = 20, MessageId = 5, Reason = "spam" };
        _mockMessageService.Setup(s => s.ReportAbuseAsync(CurrentUserId, It.IsAny<ReportMessageDto>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.ReportAbuse(dto);
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task ReportAbuse_AsOther_ReturnsForbid()
    {
        var dto = new ReportAbuseRequestDto { UserId = 99, ReportedUserId = 20, MessageId = 5, Reason = "spam" };
        var result = await _controller.ReportAbuse(dto);
        result.Should().BeOfType<ForbidResult>();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static ControllerContext MakeAuthContext(int userId)
    {
        var claims = new List<Claim> { new("userId", userId.ToString()) };
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
        };
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
        return new ControllerContext { HttpContext = httpContext };
    }
}
