using Lander.src.Modules.Communication.Dtos.Dto;
using Lander.src.Modules.Communication.Dtos.InputDto;

namespace Lander.src.Modules.Communication.Intefaces;

public interface IReportService
{
    Task<List<ReportedMessageDto>> GetAllReportsAsync(string? status = null);
    Task<bool> ReviewReportAsync(int reportId, UpdateReportStatusDto dto, int adminId);
    Task<bool> ResolveReportAsync(int reportId, UpdateReportStatusDto dto, int adminId);
    Task<bool> DeleteReportAsync(int reportId);
}
