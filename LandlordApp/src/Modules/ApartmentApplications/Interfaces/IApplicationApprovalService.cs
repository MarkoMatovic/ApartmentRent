using Lander.src.Modules.ApartmentApplications.Models;

namespace Lander.src.Modules.ApartmentApplications.Interfaces;

public interface IApplicationApprovalService
{
    Task<bool> HasApprovedApplicationAsync(int userId, int apartmentId);
    
    Task<ApartmentApplication?> GetApplicationAsync(int userId, int apartmentId);
}
