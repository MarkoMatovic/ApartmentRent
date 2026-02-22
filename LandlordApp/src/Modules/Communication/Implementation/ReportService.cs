using Lander.src.Modules.Communication.Dtos.Dto;
using Lander.src.Modules.Communication.Dtos.InputDto;
using Lander.src.Modules.Communication.Intefaces;
using Lander.src.Modules.Communication.Models;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Communication.Implementation;

public class ReportService : IReportService
{
    private readonly CommunicationsContext _context;
    private readonly UsersContext _usersContext;

    public ReportService(CommunicationsContext context, UsersContext usersContext)
    {
        _context = context;
        _usersContext = usersContext;
    }

    public async Task<List<ReportedMessageDto>> GetAllReportsAsync(string? status = null)
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

        return result;
    }

    public async Task<bool> ReviewReportAsync(int reportId, UpdateReportStatusDto dto, int adminId)
    {
        var report = await _context.ReportedMessages.FindAsync(reportId);
        if (report == null) return false;

        report.Status = "Reviewed";
        report.ReviewedByAdminId = adminId;
        report.ReviewedDate = DateTime.UtcNow;
        report.AdminNotes = dto.AdminNotes;
        report.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResolveReportAsync(int reportId, UpdateReportStatusDto dto, int adminId)
    {
        var report = await _context.ReportedMessages.FindAsync(reportId);
        if (report == null) return false;

        report.Status = "Resolved";
        report.ReviewedByAdminId = adminId;
        report.ReviewedDate = DateTime.UtcNow;
        report.AdminNotes = dto.AdminNotes;
        report.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteReportAsync(int reportId)
    {
        var report = await _context.ReportedMessages.FindAsync(reportId);
        if (report == null) return false;

        _context.ReportedMessages.Remove(report);
        await _context.SaveChangesAsync();
        return true;
    }
}
