using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Lander.src.Notifications.Controllers;
using Lander.src.Notifications.Dtos.Dto;
using Lander.src.Notifications.Dtos.InputDto;
using Lander.src.Notifications.Interfaces;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using System.Security.Claims;

namespace LandlordApp.Tests.Controllers;

public class NotificationsControllerTests
{
    private readonly Mock<INotificationService> _mockService;
    private readonly Mock<IUserInterface> _mockUserService;
    private readonly NotificationsController _controller;
    private const int CurrentUserId = 5;

    private static readonly NotificationDto SampleNotification = new()
    {
        Id = 1, SenderUserId = 5, Message = "Test"
    };

    public NotificationsControllerTests()
    {
        _mockService = new Mock<INotificationService>();
        _mockUserService = new Mock<IUserInterface>();
        _controller = new NotificationsController(_mockService.Object, _mockUserService.Object);
        _controller.ControllerContext = MakeAuthContext(CurrentUserId);
    }

    private static ControllerContext MakeAuthContext(int userId)
    {
        var claims = new List<Claim> { new("userId", userId.ToString()) };
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
        };
        return new ControllerContext { HttpContext = httpContext };
    }

    // ─── GetUserNotifications ─────────────────────────────────────────────────

    [Fact]
    public async Task GetUserNotifications_ReturnsOk()
    {
        _mockService.Setup(s => s.GetUserNotificationsAsync(5))
            .ReturnsAsync(new List<NotificationDto> { SampleNotification });

        var result = await _controller.GetUserNotifications(5);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeAssignableTo<IEnumerable<NotificationDto>>();
    }

    [Fact]
    public async Task GetUserNotifications_Empty_ReturnsOkWithEmptyList()
    {
        // Caller may only read their own notifications — authenticate as user 99
        _controller.ControllerContext = MakeAuthContext(99);
        _mockService.Setup(s => s.GetUserNotificationsAsync(99))
            .ReturnsAsync(new List<NotificationDto>());

        var result = await _controller.GetUserNotifications(99);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetUserNotifications_OtherUsersId_ReturnsForbid()
    {
        var result = await _controller.GetUserNotifications(99);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetUserNotifications_ServiceThrows_PropagatesException()
    {
        _mockService.Setup(s => s.GetUserNotificationsAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("DB error"));

        Func<Task> act = async () => await _controller.GetUserNotifications(5);

        await act.Should().ThrowAsync<Exception>().WithMessage("DB error");
    }

    // ─── SendNotification ─────────────────────────────────────────────────────

    [Fact]
    public async Task SendNotification_ReturnsOk()
    {
        var input = new CreateNotificationInputDto { RecipientUserId = 5, Message = "Hello" };
        _mockService.Setup(s => s.SendNotificationAsync(input)).ReturnsAsync(SampleNotification);

        var result = await _controller.SendNotification(input);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(SampleNotification);
    }

    [Fact]
    public async Task SendNotification_ServiceThrows_PropagatesException()
    {
        var input = new CreateNotificationInputDto { RecipientUserId = 5, Message = "Hello" };
        _mockService.Setup(s => s.SendNotificationAsync(input))
            .ThrowsAsync(new Exception("Send error"));

        Func<Task> act = async () => await _controller.SendNotification(input);

        await act.Should().ThrowAsync<Exception>().WithMessage("Send error");
    }

    // ─── MarkRead ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task MarkRead_ReturnsOk()
    {
        // Controller verifies the caller is the recipient before marking as read
        _mockService.Setup(s => s.GetNotificationByIdAsync(1))
            .ReturnsAsync(new NotificationDto { Id = 1, RecipientUserId = CurrentUserId });
        _mockService.Setup(s => s.MarkAsReadAsync(1)).Returns(Task.CompletedTask);

        var result = await _controller.MarkRead(1);

        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task MarkRead_NotRecipient_ReturnsForbid()
    {
        _mockService.Setup(s => s.GetNotificationByIdAsync(1))
            .ReturnsAsync(new NotificationDto { Id = 1, RecipientUserId = CurrentUserId + 1 });

        var result = await _controller.MarkRead(1);

        result.Should().BeOfType<ForbidResult>();
        _mockService.Verify(s => s.MarkAsReadAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task MarkRead_ServiceThrows_PropagatesException()
    {
        _mockService.Setup(s => s.GetNotificationByIdAsync(1))
            .ReturnsAsync(new NotificationDto { Id = 1, RecipientUserId = CurrentUserId });
        _mockService.Setup(s => s.MarkAsReadAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("Mark error"));

        Func<Task> act = async () => await _controller.MarkRead(1);

        await act.Should().ThrowAsync<Exception>().WithMessage("Mark error");
    }

    // ─── DeleteNotification ───────────────────────────────────────────────────

    [Fact]
    public async Task DeleteNotification_Found_ReturnsOk()
    {
        // Controller verifies the caller is the recipient before deleting
        _mockService.Setup(s => s.GetNotificationByIdAsync(1))
            .ReturnsAsync(new NotificationDto { Id = 1, RecipientUserId = CurrentUserId });
        _mockService.Setup(s => s.DeleteNotificationAsync(1)).ReturnsAsync(true);

        var result = await _controller.DeleteNotification(1);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(true);
    }

    [Fact]
    public async Task DeleteNotification_NotFound_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetNotificationByIdAsync(99)).ReturnsAsync((NotificationDto?)null);

        var result = await _controller.DeleteNotification(99);

        result.Result.Should().BeOfType<NotFoundResult>();
        _mockService.Verify(s => s.DeleteNotificationAsync(It.IsAny<int>()), Times.Never);
    }

    // ─── MarkAllAsRead ────────────────────────────────────────────────────────

    [Fact]
    public async Task MarkAllAsRead_ReturnsOk()
    {
        _mockService.Setup(s => s.MarkAllAsReadAsync(CurrentUserId)).ReturnsAsync(true);

        var result = await _controller.MarkAllAsRead();

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(true);
    }

    [Fact]
    public async Task MarkAllAsRead_ReturnsFalse_ReturnsOkFalse()
    {
        _mockService.Setup(s => s.MarkAllAsReadAsync(CurrentUserId)).ReturnsAsync(false);

        var result = await _controller.MarkAllAsRead();

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(false);
    }
}
