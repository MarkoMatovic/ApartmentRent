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

namespace LandlordApp.Tests.Controllers;

public class NotificationsControllerTests
{
    private readonly Mock<INotificationService> _mockService;
    private readonly NotificationsController _controller;

    private static readonly NotificationDto SampleNotification = new()
    {
        Id = 1, SenderUserId = 5, Message = "Test"
    };

    public NotificationsControllerTests()
    {
        _mockService = new Mock<INotificationService>();
        _controller = new NotificationsController(_mockService.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
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
        _mockService.Setup(s => s.GetUserNotificationsAsync(99))
            .ReturnsAsync(new List<NotificationDto>());

        var result = await _controller.GetUserNotifications(99);

        result.Result.Should().BeOfType<OkObjectResult>();
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

    // ─── MarkRead ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task MarkRead_ReturnsOk()
    {
        _mockService.Setup(s => s.MarkAsReadAsync(1)).Returns(Task.CompletedTask);

        var result = await _controller.MarkRead(1);

        result.Should().BeOfType<OkResult>();
    }

    // ─── DeleteNotification ───────────────────────────────────────────────────

    [Fact]
    public async Task DeleteNotification_Found_ReturnsOk()
    {
        _mockService.Setup(s => s.DeleteNotificationAsync(1)).ReturnsAsync(true);

        var result = await _controller.DeleteNotification(1);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(true);
    }

    [Fact]
    public async Task DeleteNotification_NotFound_ReturnsNotFound()
    {
        _mockService.Setup(s => s.DeleteNotificationAsync(99)).ReturnsAsync(false);

        var result = await _controller.DeleteNotification(99);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ─── MarkAllAsRead ────────────────────────────────────────────────────────

    [Fact]
    public async Task MarkAllAsRead_ReturnsOk()
    {
        _mockService.Setup(s => s.MarkAllAsReadAsync(5)).ReturnsAsync(true);

        var result = await _controller.MarkAllAsRead(5);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(true);
    }
}
