using Lander.src.Modules.ApartmentApplications.Interfaces;
using Lander.src.Modules.ApartmentApplications.Models;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.ApartmentApplications.Implementation;

public class ApplicationApprovalService : IApplicationApprovalService
{
    private readonly ApplicationsContext _context;

    public ApplicationApprovalService(ApplicationsContext context)
    {
        _context = context;
    }

    public async Task<bool> HasApprovedApplicationAsync(int userId, int apartmentId)
    {
        var application = await _context.ApartmentApplications
            .FirstOrDefaultAsync(a => 
                a.UserId == userId && 
                a.ApartmentId == apartmentId && 
                a.Status == "Approved");

        return application != null;
    }

    public async Task<ApartmentApplication?> GetApplicationAsync(int userId, int apartmentId)
    {
        return await _context.ApartmentApplications
            .FirstOrDefaultAsync(a =>
                a.UserId == userId &&
                a.ApartmentId == apartmentId);
    }

    public async Task<ApprovalStatusResult> GetApprovalStatusAsync(int userId, int apartmentId)
    {
        var application = await _context.ApartmentApplications
            .FirstOrDefaultAsync(a =>
                a.UserId == userId &&
                a.ApartmentId == apartmentId);

        return new ApprovalStatusResult(
            HasApprovedApplication: application?.Status == "Approved",
            ApplicationStatus: application?.Status,
            ApplicationId: application?.ApplicationId);
    }
}
