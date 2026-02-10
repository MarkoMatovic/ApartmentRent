using Lander.src.Modules.ApartmentApplications.Models;
using Lander.src.Common;

namespace Lander.src.Modules.ApartmentApplications.Interfaces;

public interface IApartmentApplicationService
{
    Task<ApartmentApplication?> ApplyForApartmentAsync(int userId, int apartmentId);
    Task<List<ApartmentApplication>> GetLandlordApplicationsAsync(int landlordId);
    Task<List<ApartmentApplication>> GetTenantApplicationsAsync(int tenantId);
    Task<ApartmentApplication?> UpdateApplicationStatusAsync(int applicationId, string status, int landlordUserId);
    Task<ApartmentApplication?> GetApplicationByIdAsync(int applicationId);
}
