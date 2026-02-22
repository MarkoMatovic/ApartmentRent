using Lander.src.Modules.Communication.Dtos.Dto;
using Lander.src.Modules.Communication.Dtos.InputDto;
using Lander.src.Modules.Communication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Lander.src.Modules.Communication.Intefaces;

namespace Lander.src.Modules.Communication.Controllers;

[Route("api/v1/reports")]
[ApiController]
[Authorize(Roles = "Admin")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ReportedMessageDto>>> GetAllReports([FromQuery] string? status = null)
    {
        var reports = await _reportService.GetAllReportsAsync(status);
        return Ok(reports);
    }

    [HttpPut("{reportId}/review")]
    public async Task<IActionResult> ReviewReport(int reportId, [FromBody] UpdateReportStatusDto dto, [FromQuery] int adminId)
    {
        var success = await _reportService.ReviewReportAsync(reportId, dto, adminId);
        if (!success) return NotFound();
        return Ok();
    }

    [HttpPut("{reportId}/resolve")]
    public async Task<IActionResult> ResolveReport(int reportId, [FromBody] UpdateReportStatusDto dto, [FromQuery] int adminId)
    {
        var success = await _reportService.ResolveReportAsync(reportId, dto, adminId);
        if (!success) return NotFound();
        return Ok();
    }

    [HttpDelete("{reportId}")]
    public async Task<IActionResult> DeleteReport(int reportId)
    {
        var success = await _reportService.DeleteReportAsync(reportId);
        if (!success) return NotFound();
        return Ok();
    }
}
