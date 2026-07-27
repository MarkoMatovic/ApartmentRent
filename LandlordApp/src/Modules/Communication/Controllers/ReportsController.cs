using System.Security.Claims;
using Lander.Helpers;
using Lander.src.Modules.Communication.Dtos.Dto;
using Lander.src.Modules.Communication.Dtos.InputDto;
using Lander.src.Modules.Communication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lander.src.Modules.Communication.Interfaces;

namespace Lander.src.Modules.Communication.Controllers;

[Route(ApiActionsV1.Reports)]
[ApiController]
[Authorize(Roles = "Admin")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    private int? GetAdminId()
    {
        var claim = User.FindFirstValue("userId");
        return int.TryParse(claim, out var id) ? id : null;
    }

    [HttpGet]
    public async Task<ActionResult<List<ReportedMessageDto>>> GetAllReports([FromQuery] string? status = null)
    {
        var reports = await _reportService.GetAllReportsAsync(status);
        return Ok(reports);
    }

    [HttpPut(ApiActionsV1.ReviewReport, Name = nameof(ApiActionsV1.ReviewReport))]
    public async Task<IActionResult> ReviewReport(int reportId, [FromBody] UpdateReportStatusDto dto)
    {
        var adminId = GetAdminId();
        if (adminId is null) return Unauthorized();
        var success = await _reportService.ReviewReportAsync(reportId, dto, adminId.Value);
        if (!success) return NotFound();
        return Ok();
    }

    [HttpPut(ApiActionsV1.ResolveReport, Name = nameof(ApiActionsV1.ResolveReport))]
    public async Task<IActionResult> ResolveReport(int reportId, [FromBody] UpdateReportStatusDto dto)
    {
        var adminId = GetAdminId();
        if (adminId is null) return Unauthorized();
        var success = await _reportService.ResolveReportAsync(reportId, dto, adminId.Value);
        if (!success) return NotFound();
        return Ok();
    }

    [HttpDelete(ApiActionsV1.DeleteReport, Name = nameof(ApiActionsV1.DeleteReport))]
    public async Task<IActionResult> DeleteReport(int reportId)
    {
        var success = await _reportService.DeleteReportAsync(reportId);
        if (!success) return NotFound();
        return Ok();
    }
}
