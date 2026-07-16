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

        _controller = new AppointmentsController(_mockService.Object);
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
    public async Task CreateAppointment_ArgumentException_PropagatesToMiddleware()
    {
        // Only UnauthorizedAccessException is handled in the controller —
        // everything else goes to the global exception middleware.
        var dto = new CreateAppointmentDto { ApartmentId = 10 };
        _mockService.Setup(s => s.CreateAppointmentAsync(dto))
            .ThrowsAsync(new ArgumentException("Slot not available"));

        var act = () => _controller.CreateAppointment(dto);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Slot not available");
    }

    [Fact]
    public async Task CreateAppointment_InvalidOperationException_PropagatesToMiddleware()
    {
        var dto = new CreateAppointmentDto { ApartmentId = 10 };
        _mockService.Setup(s => s.CreateAppointmentAsync(dto))
            .ThrowsAsync(new InvalidOperationException("Already booked"));

        var act = () => _controller.CreateAppointment(dto);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Already booked");
    }

    [Fact]
    public async Task CreateAppointment_UnexpectedException_PropagatesToMiddleware()
    {
        var dto = new CreateAppointmentDto { ApartmentId = 10 };
        _mockService.Setup(s => s.CreateAppointmentAsync(dto))
            .ThrowsAsync(new Exception("DB error"));

        var act = () => _controller.CreateAppointment(dto);

        await act.Should().ThrowAsync<Exception>().WithMessage("DB error");
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
    public async Task GetMyAppointments_ServiceThrows_PropagatesToMiddleware()
    {
        _mockService.Setup(s => s.GetMyAppointmentsAsync()).ThrowsAsync(new Exception("fail"));

        var act = () => _controller.GetMyAppointments();

        await act.Should().ThrowAsync<Exception>().WithMessage("fail");
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
    public async Task GetLandlordAppointments_ServiceThrows_PropagatesToMiddleware()
    {
        _mockService.Setup(s => s.GetLandlordAppointmentsAsync())
            .ThrowsAsync(new Exception("DB error"));

        var act = () => _controller.GetLandlordAppointments();

        await act.Should().ThrowAsync<Exception>().WithMessage("DB error");
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
    public async Task GetAvailableSlots_ArgumentException_PropagatesToMiddleware()
    {
        _mockService.Setup(s => s.GetAvailableSlotsAsync(10, It.IsAny<DateTime>()))
            .ThrowsAsync(new ArgumentException("Apartment not available"));

        var act = () => _controller.GetAvailableSlots(10, new DateTime(2026, 6, 15));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Apartment not available");
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
    public async Task UpdateAppointmentStatus_ArgumentException_PropagatesToMiddleware()
    {
        var dto = new UpdateAppointmentStatusDto { Status = AppointmentStatus.Pending };
        _mockService.Setup(s => s.UpdateAppointmentStatusAsync(1, dto))
            .ThrowsAsync(new ArgumentException("Invalid status"));

        var act = () => _controller.UpdateAppointmentStatus(1, dto);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Invalid status");
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
    public async Task CancelAppointment_ArgumentException_PropagatesToMiddleware()
    {
        _mockService.Setup(s => s.CancelAppointmentAsync(1))
            .ThrowsAsync(new ArgumentException("Not found"));

        var act = () => _controller.CancelAppointment(1);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Not found");
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
    public async Task GetMyAvailability_UnauthorizedAccess_PropagatesToMiddleware()
    {
        _mockService.Setup(s => s.GetMyAvailabilityAsync())
            .ThrowsAsync(new UnauthorizedAccessException("not a landlord"));

        var act = () => _controller.GetMyAvailability();

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
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
    public async Task SetMyAvailability_UnauthorizedAccess_PropagatesToMiddleware()
    {
        var dto = new SetAvailabilityDto();
        _mockService.Setup(s => s.SetMyAvailabilityAsync(dto))
            .ThrowsAsync(new UnauthorizedAccessException("not a landlord"));

        var act = () => _controller.SetMyAvailability(dto);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
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
