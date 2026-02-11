using Lander.src.Modules.Communication.Dtos.Dto;
using Lander.src.Modules.Communication.Dtos.InputDto;
using Lander.src.Modules.Communication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Communication.Controllers;

[Route("api/v1/reports")]
[ApiController]
[Authorize(Roles = "Admin")]
public class ReportsController : ControllerBase
{
    private readonly CommunicationsContext _context;
    private readonly UsersContext _usersContext;

    public ReportsController(CommunicationsContext context, UsersContext usersContext)
    {
        _context = context;
        _usersContext = usersContext;
    }

    [HttpGet]
    public async Task<ActionResult<List<ReportedMessageDto>>> GetAllReports([FromQuery] string? status = null)
    {
        var query = _context.ReportedMessages
            .Include(r => r.Message)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(r => r.Status == status);
        }

        var reports = await query
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();

        var result = new List<ReportedMessageDto>();
        var userIds = reports
            .SelectMany(r => new[] { r.ReportedByUserId, r.ReportedUserId, r.ReviewedByAdminId ?? 0 })
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        var users = await _usersContext.Users
            .Where(u => userIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId);

        foreach (var report in reports)
        {
            var dto = new ReportedMessageDto
            {
                ReportId = report.ReportId,
                MessageId = report.MessageId,
                MessageText = report.Message?.MessageText ?? "",
                ReportedByUserId = report.ReportedByUserId,
                ReportedByUserName = users.TryGetValue(report.ReportedByUserId, out var reportedBy)
                    ? $"{reportedBy.FirstName} {reportedBy.LastName}"
                    : "Unknown",
                ReportedUserId = report.ReportedUserId,
                ReportedUserName = users.TryGetValue(report.ReportedUserId, out var reportedUser)
                    ? $"{reportedUser.FirstName} {reportedUser.LastName}"
                    : "Unknown",
                Reason = report.Reason,
                Status = report.Status,
                CreatedDate = report.CreatedDate ?? DateTime.UtcNow,
                ReviewedByAdminId = report.ReviewedByAdminId,
                ReviewedByAdminName = report.ReviewedByAdminId.HasValue && users.TryGetValue(report.ReviewedByAdminId.Value, out var admin)
                    ? $"{admin.FirstName} {admin.LastName}"
                    : null,
                ReviewedDate = report.ReviewedDate,
                AdminNotes = report.AdminNotes
            };
            result.Add(dto);
        }

        return Ok(result);
    }

    [HttpPut("{reportId}/review")]
    public async Task<IActionResult> ReviewReport(int reportId, [FromBody] UpdateReportStatusDto dto, [FromQuery] int adminId)
    {
        var report = await _context.ReportedMessages.FindAsync(reportId);
        if (report == null)
            return NotFound();

        report.Status = "Reviewed";
        report.ReviewedByAdminId = adminId;
        report.ReviewedDate = DateTime.UtcNow;
        report.AdminNotes = dto.AdminNotes;
        report.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("{reportId}/resolve")]
    public async Task<IActionResult> ResolveReport(int reportId, [FromBody] UpdateReportStatusDto dto, [FromQuery] int adminId)
    {
        var report = await _context.ReportedMessages.FindAsync(reportId);
        if (report == null)
            return NotFound();

        report.Status = "Resolved";
        report.ReviewedByAdminId = adminId;
        report.ReviewedDate = DateTime.UtcNow;
        report.AdminNotes = dto.AdminNotes;
        report.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{reportId}")]
    public async Task<IActionResult> DeleteReport(int reportId)
    {
        var report = await _context.ReportedMessages.FindAsync(reportId);
        if (report == null)
            return NotFound();

        _context.ReportedMessages.Remove(report);
        await _context.SaveChangesAsync();
        return Ok();
    }
}
