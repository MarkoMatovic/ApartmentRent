using Lander.src.Modules.ApartmentApplications.Interfaces;
using Lander.src.Modules.ApartmentApplications.Models;
using Lander.src.Modules.Listings.Interfaces;
using Lander.src.Notifications.Interfaces; // Assuming NotificationService interface exists
using Lander.src.Notifications.NotificationsHub;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.ApartmentApplications.Implementation;

public class ApartmentApplicationService : IApartmentApplicationService
{
    private readonly ApplicationsContext _context;
    private readonly IApartmentService _apartmentService; // To verify landlord ownership
    private readonly IHubContext<NotificationHub> _notificationHub;

    public ApartmentApplicationService(
        ApplicationsContext context, 
        IApartmentService apartmentService,
        IHubContext<NotificationHub> notificationHub)
    {
        _context = context;
        _apartmentService = apartmentService;
        _notificationHub = notificationHub;
    }

    public async Task<ApartmentApplication?> ApplyForApartmentAsync(int userId, int apartmentId)
    {
        // Check if already applied
        var existing = await _context.ApartmentApplications
            .FirstOrDefaultAsync(a => a.UserId == userId && a.ApartmentId == apartmentId);

        if (existing != null) return null; // Already applied

        var application = new ApartmentApplication
        {
            UserId = userId,
            ApartmentId = apartmentId,
            ApplicationDate = DateTime.UtcNow,
            Status = "Pending",
            CreatedDate = DateTime.UtcNow,
            CreatedByGuid = Guid.NewGuid(), // Placeholder or fetch actual GUID if available
        };

        _context.ApartmentApplications.Add(application);
        await _context.SaveChangesAsync();
        
        // Notify Landlord (Ideally we need LandlordId here, but ApartmentApplication model might not have it directly on the entity without join)
        // For Phase 3 MVP, we will rely on the Controller to fetch extra details if needed for specific notifications,
        // or we fetch the apartment to get the landlord ID.
        var apartment = await _apartmentService.GetApartmentByIdAsync(apartmentId);
        if (apartment != null && apartment.LandlordId.HasValue)
        {
             await _notificationHub.Clients.Group(apartment.LandlordId.Value.ToString()).SendAsync("ReceiveNotification", 
                "New Application!", 
                $"You have a new application for '{apartment.Title}'.", 
                "info");
        }

        return application;
    }

    public async Task<List<ApartmentApplication>> GetLandlordApplicationsAsync(int landlordId)
    {
        // Get all apartments owned by landlord
        var apartments = await _apartmentService.GetApartmentsByLandlordIdAsync(landlordId);
        var apartmentIds = apartments.Select(a => a.ApartmentId).ToList();

        // Don't use .Include() for cross-context entities (Apartment, User)
        // Frontend will need to fetch apartment/user details separately if needed
        return await _context.ApartmentApplications
            .Where(a => a.ApartmentId.HasValue && apartmentIds.Contains(a.ApartmentId.Value))
            .OrderByDescending(a => a.ApplicationDate)
            .ToListAsync();
    }

    public async Task<List<ApartmentApplication>> GetTenantApplicationsAsync(int tenantId)
    {
        // Don't use .Include() for cross-context entities (Apartment)
        // Frontend will need to fetch apartment details separately if needed
        return await _context.ApartmentApplications
            .Where(a => a.UserId == tenantId)
            .OrderByDescending(a => a.ApplicationDate)
            .ToListAsync();
    }

    public async Task<ApartmentApplication?> GetApplicationByIdAsync(int applicationId)
    {
        // Don't use .Include() for cross-context entities
        return await _context.ApartmentApplications
            .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);
    }

    public async Task<ApartmentApplication?> UpdateApplicationStatusAsync(int applicationId, string status, int landlordUserId)
    {
        var application = await _context.ApartmentApplications
            .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

        if (application == null) return null;

        // Verify Landlord owns this apartment
        if (application.ApartmentId.HasValue)
        {
            var apartment = await _apartmentService.GetApartmentByIdAsync(application.ApartmentId.Value);
            if (apartment == null || apartment.LandlordId != landlordUserId)
            {
                throw new UnauthorizedAccessException("User is not the landlord of this apartment.");
            }
        }

        application.Status = status;
        application.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Notify Tenant
        if (application.UserId.HasValue)
        {
             await _notificationHub.Clients.Group(application.UserId.Value.ToString()).SendAsync("ReceiveNotification", 
                $"Application {status}", 
                $"Your application status has been updated to: {status}.", 
                status == "Approved" ? "success" : "info");
        }

        return application;
    }
}
