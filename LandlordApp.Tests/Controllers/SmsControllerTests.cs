using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Lander.src.Modules.Communication.Controllers;
using Lander.src.Modules.Communication.Dtos.Dto;
using Lander.src.Modules.Communication.Dtos.InputDto;
using Lander.src.Modules.Communication.Interfaces;

namespace LandlordApp.Tests.Controllers;

public class SmsControllerTests
{
    private readonly Mock<ISmsService> _mockSmsService;
    private readonly SmsController _controller;
    private const int CurrentUserId = 1;

    public SmsControllerTests()
    {
        _mockSmsService = new Mock<ISmsService>();

        _controller = new SmsController(_mockSmsService.Object);
        _controller.ControllerContext = MakeAuthContext(CurrentUserId);
    }

    // ─── SendSms ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendSms_SuccessfulSend_ReturnsOkWithDto()
    {
        var input = new SendSmsInputDto
        {
            ToPhoneNumber = "+381601234567",
            MessageText = "Hello!",
            SenderId = CurrentUserId,
            ReceiverId = 2
        };
        var response = new SendSmsDto { Success = true, Message = "SMS sent successfully" };
        _mockSmsService.Setup(s => s.SendSmsAsync(input))
            .ReturnsAsync(response);

        var result = await _controller.SendSms(input);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task SendSms_ServiceReturnsFailed_ReturnsBadRequest()
    {
        var input = new SendSmsInputDto
        {
            ToPhoneNumber = "+381601234567",
            MessageText = "Hello!",
            SenderId = CurrentUserId,
            ReceiverId = 2
        };
        var response = new SendSmsDto { Success = false, Message = "Failed to send SMS" };
        _mockSmsService.Setup(s => s.SendSmsAsync(It.IsAny<SendSmsInputDto>()))
            .ReturnsAsync(response);

        var result = await _controller.SendSms(input);

        result.Result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task SendSms_ServiceThrows_PropagatesException()
    {
        var input = new SendSmsInputDto
        {
            ToPhoneNumber = "+381601234567",
            MessageText = "Test",
            SenderId = CurrentUserId,
            ReceiverId = 3
        };
        _mockSmsService.Setup(s => s.SendSmsAsync(It.IsAny<SendSmsInputDto>()))
            .ThrowsAsync(new Exception("SMS provider error"));

        Func<Task> act = async () => await _controller.SendSms(input);

        await act.Should().ThrowAsync<Exception>().WithMessage("SMS provider error");
    }

    [Fact]
    public async Task SendSms_EmptyPhoneNumber_ServiceReturnsFailure_ReturnsBadRequest()
    {
        var input = new SendSmsInputDto
        {
            ToPhoneNumber = "",
            MessageText = "Test",
            SenderId = CurrentUserId,
            ReceiverId = 2
        };
        var response = new SendSmsDto { Success = false, Message = "Invalid phone number" };
        _mockSmsService.Setup(s => s.SendSmsAsync(It.IsAny<SendSmsInputDto>()))
            .ReturnsAsync(response);

        var result = await _controller.SendSms(input);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SendSms_CallsServiceOnce()
    {
        var input = new SendSmsInputDto
        {
            ToPhoneNumber = "+381601234567",
            MessageText = "Hello!",
            SenderId = CurrentUserId,
            ReceiverId = 2
        };
        _mockSmsService.Setup(s => s.SendSmsAsync(input))
            .ReturnsAsync(new SendSmsDto { Success = true, Message = "OK" });

        await _controller.SendSms(input);

        _mockSmsService.Verify(s => s.SendSmsAsync(input), Times.Once);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static ControllerContext MakeAuthContext(int userId = 1, Guid? userGuid = null)
    {
        userGuid ??= Guid.NewGuid();
        var claims = new List<Claim>
        {
            new("userId", userId.ToString()),
            new("sub", userGuid.ToString())
        };
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
        };
        return new ControllerContext { HttpContext = httpContext };
    }
}
