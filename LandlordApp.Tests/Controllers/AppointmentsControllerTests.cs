using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Lander.src.Modules.Appointments.Controllers;
using Lander.src.Modules.Appointments.Dtos;
using Lander.src.Modules.Appointments.Interfaces;
using Lander.src.Modules.Appointments.Models;

namespace LandlordApp.Tests.Controllers;

public class AppointmentsControllerTests
{
    private readonly Mock<IAppointmentService> _mockService;
    private readonly Mock<ILogger<AppointmentsController>> _mockLogger;
    private readonly AppointmentsController _controller;

    private static readonly AppointmentDto SampleAppointment = new()
    {
        AppointmentId = 1, ApartmentId = 10
    };

    public AppointmentsControllerTests()
    {
        _mockService = new Mock<IAppointmentService>();
        _mockLogger = new Mock<ILogger<AppointmentsController>>();

        _controller = new AppointmentsController(_mockService.Object, _mockLogger.Object);
        _controller.ControllerContext = MakeAuthContext(1);
    }

    // ─── CreateAppointment ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAppointment_ReturnsOk()
    {
        var dto = new CreateAppointmentDto { ApartmentId = 10, AppointmentDate = DateTime.UtcNow.AddDays(1) };
        _mockService.Setup(s => s.CreateAppointmentAsync(dto)).ReturnsAsync(SampleAppointment);

        var result = await _controller.CreateAppointment(dto);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(SampleAppointment);
    }

    [Fact]
    public async Task CreateAppointment_ArgumentException_ReturnsBadRequest()
    {
        var dto = new CreateAppointmentDto { ApartmentId = 10 };
        _mockService.Setup(s => s.CreateAppointmentAsync(dto))
            .ThrowsAsync(new ArgumentException("Slot not available"));

        var result = await _controller.CreateAppointment(dto);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateAppointment_InvalidOperationException_ReturnsConflict()
    {
        var dto = new CreateAppointmentDto { ApartmentId = 10 };
        _mockService.Setup(s => s.CreateAppointmentAsync(dto))
            .ThrowsAsync(new InvalidOperationException("Already booked"));

        var result = await _controller.CreateAppointment(dto);

        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task CreateAppointment_UnexpectedException_Returns500()
    {
        var dto = new CreateAppointmentDto { ApartmentId = 10 };
        _mockService.Setup(s => s.CreateAppointmentAsync(dto))
            .ThrowsAsync(new Exception("DB error"));

        var result = await _controller.CreateAppointment(dto);

        result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ─── GetMyAppointments ────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyAppointments_ReturnsOk()
    {
        _mockService.Setup(s => s.GetMyAppointmentsAsync())
            .ReturnsAsync(new List<AppointmentDto> { SampleAppointment });

        var result = await _controller.GetMyAppointments();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMyAppointments_ServiceThrows_Returns500()
    {
        _mockService.Setup(s => s.GetMyAppointmentsAsync()).ThrowsAsync(new Exception("fail"));

        var result = await _controller.GetMyAppointments();

        result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ─── GetLandlordAppointments ──────────────────────────────────────────────

    [Fact]
    public async Task GetLandlordAppointments_ReturnsOk()
    {
        _mockService.Setup(s => s.GetLandlordAppointmentsAsync())
            .ReturnsAsync(new List<AppointmentDto>());

        var result = await _controller.GetLandlordAppointments();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetLandlordAppointments_ServiceThrows_Returns500()
    {
        _mockService.Setup(s => s.GetLandlordAppointmentsAsync())
            .ThrowsAsync(new Exception("DB error"));

        var result = await _controller.GetLandlordAppointments();

        result.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(500);
    }

    // ─── GetAvailableSlots ────────────────────────────────────────────────────

    [Fact]
    public async Task GetAvailableSlots_ValidDate_ReturnsOk()
    {
        _mockService.Setup(s => s.GetAvailableSlotsAsync(10, It.IsAny<DateTime>()))
            .ReturnsAsync(new List<AvailableSlotDto>());

        var result = await _controller.GetAvailableSlots(10, new DateTime(2026, 6, 15));

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAvailableSlots_ArgumentException_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.GetAvailableSlotsAsync(10, It.IsAny<DateTime>()))
            .ThrowsAsync(new ArgumentException("Apartment not available"));

        var result = await _controller.GetAvailableSlots(10, new DateTime(2026, 6, 15));

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ─── UpdateAppointmentStatus ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateAppointmentStatus_ReturnsOk()
    {
        var dto = new UpdateAppointmentStatusDto { Status = AppointmentStatus.Confirmed };
        _mockService.Setup(s => s.UpdateAppointmentStatusAsync(1, dto)).ReturnsAsync(SampleAppointment);

        var result = await _controller.UpdateAppointmentStatus(1, dto);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(SampleAppointment);
    }

    [Fact]
    public async Task UpdateAppointmentStatus_ArgumentException_ReturnsBadRequest()
    {
        var dto = new UpdateAppointmentStatusDto { Status = AppointmentStatus.Pending };
        _mockService.Setup(s => s.UpdateAppointmentStatusAsync(1, dto))
            .ThrowsAsync(new ArgumentException("Invalid status"));

        var result = await _controller.UpdateAppointmentStatus(1, dto);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateAppointmentStatus_UnauthorizedAccess_ReturnsForbid()
    {
        var dto = new UpdateAppointmentStatusDto { Status = AppointmentStatus.Confirmed };
        _mockService.Setup(s => s.UpdateAppointmentStatusAsync(1, dto))
            .ThrowsAsync(new UnauthorizedAccessException("not your appointment"));

        var result = await _controller.UpdateAppointmentStatus(1, dto);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    // ─── CancelAppointment ────────────────────────────────────────────────────

    [Fact]
    public async Task CancelAppointment_ReturnsNoContent()
    {
        _mockService.Setup(s => s.CancelAppointmentAsync(1)).ReturnsAsync(true);

        var result = await _controller.CancelAppointment(1);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task CancelAppointment_ArgumentException_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.CancelAppointmentAsync(1))
            .ThrowsAsync(new ArgumentException("Not found"));

        var result = await _controller.CancelAppointment(1);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CancelAppointment_UnauthorizedAccess_ReturnsForbid()
    {
        _mockService.Setup(s => s.CancelAppointmentAsync(1))
            .ThrowsAsync(new UnauthorizedAccessException("not yours"));

        var result = await _controller.CancelAppointment(1);

        result.Should().BeOfType<ForbidResult>();
    }

    // ─── GetAppointmentById ───────────────────────────────────────────────────

    [Fact]
    public async Task GetAppointmentById_Found_ReturnsOk()
    {
        _mockService.Setup(s => s.GetAppointmentByIdAsync(1)).ReturnsAsync(SampleAppointment);

        var result = await _controller.GetAppointmentById(1);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(SampleAppointment);
    }

    [Fact]
    public async Task GetAppointmentById_NotFound_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetAppointmentByIdAsync(99)).ReturnsAsync((AppointmentDto?)null);

        var result = await _controller.GetAppointmentById(99);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ─── GetMyAvailability / SetMyAvailability ────────────────────────────────

    [Fact]
    public async Task GetMyAvailability_ReturnsOk()
    {
        _mockService.Setup(s => s.GetMyAvailabilityAsync())
            .ReturnsAsync(new List<LandlordAvailabilityDto>());

        var result = await _controller.GetMyAvailability();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMyAvailability_UnauthorizedAccess_ReturnsUnauthorized()
    {
        _mockService.Setup(s => s.GetMyAvailabilityAsync())
            .ThrowsAsync(new UnauthorizedAccessException("not a landlord"));

        var result = await _controller.GetMyAvailability();

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task SetMyAvailability_ReturnsOk()
    {
        var dto = new SetAvailabilityDto();
        _mockService.Setup(s => s.SetMyAvailabilityAsync(dto))
            .ReturnsAsync(new List<LandlordAvailabilityDto>());

        var result = await _controller.SetMyAvailability(dto);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SetMyAvailability_UnauthorizedAccess_ReturnsUnauthorized()
    {
        var dto = new SetAvailabilityDto();
        _mockService.Setup(s => s.SetMyAvailabilityAsync(dto))
            .ThrowsAsync(new UnauthorizedAccessException("not a landlord"));

        var result = await _controller.SetMyAvailability(dto);

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static ControllerContext MakeAuthContext(int userId)
    {
        var claims = new List<Claim> { new("userId", userId.ToString()) };
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
        };
        return new ControllerContext { HttpContext = httpContext };
    }
}
