using Lander.src.Common;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.ApartmentApplications;

/// <summary>
/// Deletes all apartment applications submitted by a user when the account is deleted.
/// </summary>
public class ApplicationUserDeletedHandler : IUserDeletedHandler
{
    private readonly ApplicationsContext _context;

    public ApplicationUserDeletedHandler(ApplicationsContext context)
        => _context = context;

    public async Task HandleAsync(int userId)
    {
        var applications = await _context.ApartmentApplications
            .Where(a => a.UserId == userId)
            .ToListAsync();

        if (applications.Count > 0)
        {
            _context.ApartmentApplications.RemoveRange(applications);
            await _context.SaveChangesAsync();
        }
    }
}
