using Lander.src.Modules.ApartmentApplications.Interfaces;
using Lander.src.Modules.ApartmentApplications.Models;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.ApartmentApplications.Implementation;

/// <summary>
/// Service for checking application approval status
/// </summary>
public class ApplicationApprovalService : IApplicationApprovalService
{
    private readonly ApplicationsContext _context;

    public ApplicationApprovalService(ApplicationsContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Checks if a user has an approved application for a specific apartment
    /// </summary>
    public async Task<bool> HasApprovedApplicationAsync(int userId, int apartmentId)
    {
        var application = await _context.ApartmentApplications
            .FirstOrDefaultAsync(a => 
                a.UserId == userId && 
                a.ApartmentId == apartmentId && 
                a.Status == "Approved");

        return application != null;
    }

    /// <summary>
    /// Gets the application for a user and apartment
    /// </summary>
    public async Task<ApartmentApplication?> GetApplicationAsync(int userId, int apartmentId)
    {
        return await _context.ApartmentApplications
            .FirstOrDefaultAsync(a => 
                a.UserId == userId && 
                a.ApartmentId == apartmentId);
    }
}
