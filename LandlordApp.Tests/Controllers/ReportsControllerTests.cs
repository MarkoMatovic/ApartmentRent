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

public class ReportsControllerTests
{
    private readonly Mock<IReportService> _mockReportService;
    private readonly ReportsController _controller;
    private const int AdminUserId = 1;

    public ReportsControllerTests()
    {
        _mockReportService = new Mock<IReportService>();

        _controller = new ReportsController(_mockReportService.Object);
        _controller.ControllerContext = MakeAuthContext(AdminUserId);
    }

    // ─── GetAllReports ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllReports_NoFilter_ReturnsOkWithList()
    {
        var reports = new List<ReportedMessageDto>
        {
            new() { ReportId = 1, MessageId = 10, Reason = "spam", Status = "pending" },
            new() { ReportId = 2, MessageId = 20, Reason = "abuse", Status = "reviewed" }
        };
        _mockReportService.Setup(s => s.GetAllReportsAsync(null))
            .ReturnsAsync(reports);

        var result = await _controller.GetAllReports();

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(reports);
    }

    [Fact]
    public async Task GetAllReports_WithStatusFilter_ReturnsOkWithFilteredList()
    {
        var reports = new List<ReportedMessageDto>
        {
            new() { ReportId = 1, Reason = "spam", Status = "pending" }
        };
        _mockReportService.Setup(s => s.GetAllReportsAsync("pending"))
            .ReturnsAsync(reports);

        var result = await _controller.GetAllReports("pending");

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(reports);
    }

    [Fact]
    public async Task GetAllReports_EmptyResult_ReturnsOkWithEmptyList()
    {
        _mockReportService.Setup(s => s.GetAllReportsAsync(It.IsAny<string?>()))
            .ReturnsAsync(new List<ReportedMessageDto>());

        var result = await _controller.GetAllReports();

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeAssignableTo<IEnumerable<ReportedMessageDto>>()
            .Which.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllReports_ServiceThrows_PropagatesException()
    {
        _mockReportService.Setup(s => s.GetAllReportsAsync(It.IsAny<string?>()))
            .ThrowsAsync(new Exception("DB error"));

        Func<Task> act = async () => await _controller.GetAllReports();

        await act.Should().ThrowAsync<Exception>().WithMessage("DB error");
    }

    // ─── ReviewReport ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ReviewReport_Success_ReturnsOk()
    {
        var dto = new UpdateReportStatusDto { AdminNotes = "Under review" };
        _mockReportService.Setup(s => s.ReviewReportAsync(1, dto, AdminUserId))
            .ReturnsAsync(true);

        var result = await _controller.ReviewReport(1, dto, AdminUserId);

        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task ReviewReport_ReportNotFound_ReturnsNotFound()
    {
        var dto = new UpdateReportStatusDto { AdminNotes = "Notes" };
        _mockReportService.Setup(s => s.ReviewReportAsync(999, dto, AdminUserId))
            .ReturnsAsync(false);

        var result = await _controller.ReviewReport(999, dto, AdminUserId);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ReviewReport_ServiceThrows_PropagatesException()
    {
        var dto = new UpdateReportStatusDto();
        _mockReportService.Setup(s => s.ReviewReportAsync(It.IsAny<int>(), It.IsAny<UpdateReportStatusDto>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Review failed"));

        Func<Task> act = async () => await _controller.ReviewReport(1, dto, AdminUserId);

        await act.Should().ThrowAsync<Exception>().WithMessage("Review failed");
    }

    // ─── ResolveReport ────────────────────────────────────────────────────────

    [Fact]
    public async Task ResolveReport_Success_ReturnsOk()
    {
        var dto = new UpdateReportStatusDto { AdminNotes = "Resolved after investigation" };
        _mockReportService.Setup(s => s.ResolveReportAsync(1, dto, AdminUserId))
            .ReturnsAsync(true);

        var result = await _controller.ResolveReport(1, dto, AdminUserId);

        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task ResolveReport_ReportNotFound_ReturnsNotFound()
    {
        var dto = new UpdateReportStatusDto { AdminNotes = "Notes" };
        _mockReportService.Setup(s => s.ResolveReportAsync(999, dto, AdminUserId))
            .ReturnsAsync(false);

        var result = await _controller.ResolveReport(999, dto, AdminUserId);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ResolveReport_ServiceThrows_PropagatesException()
    {
        var dto = new UpdateReportStatusDto();
        _mockReportService.Setup(s => s.ResolveReportAsync(It.IsAny<int>(), It.IsAny<UpdateReportStatusDto>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Resolve failed"));

        Func<Task> act = async () => await _controller.ResolveReport(1, dto, AdminUserId);

        await act.Should().ThrowAsync<Exception>().WithMessage("Resolve failed");
    }

    // ─── DeleteReport ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteReport_Success_ReturnsOk()
    {
        _mockReportService.Setup(s => s.DeleteReportAsync(1))
            .ReturnsAsync(true);

        var result = await _controller.DeleteReport(1);

        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task DeleteReport_ReportNotFound_ReturnsNotFound()
    {
        _mockReportService.Setup(s => s.DeleteReportAsync(999))
            .ReturnsAsync(false);

        var result = await _controller.DeleteReport(999);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteReport_ServiceThrows_PropagatesException()
    {
        _mockReportService.Setup(s => s.DeleteReportAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("Delete failed"));

        Func<Task> act = async () => await _controller.DeleteReport(1);

        await act.Should().ThrowAsync<Exception>().WithMessage("Delete failed");
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
