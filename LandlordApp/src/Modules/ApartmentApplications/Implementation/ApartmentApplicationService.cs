using Lander.src.Modules.ApartmentApplications.Interfaces;
using Lander.src.Modules.ApartmentApplications.Models;
using Lander.src.Modules.ApartmentApplications.Dtos.Dto;
using Lander.src.Modules.Listings.Interfaces;
using Lander.src.Modules.Users.Interfaces.UserInterface;
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
    private readonly IUserInterface _userService;

    public ApartmentApplicationService(
        ApplicationsContext context, 
        IApartmentService apartmentService,
        IHubContext<NotificationHub> notificationHub,
        IUserInterface userService)
    {
        _context = context;
        _apartmentService = apartmentService;
        _notificationHub = notificationHub;
        _userService = userService;
    }

    public async Task<ApartmentApplication?> ApplyForApartmentAsync(int userId, int apartmentId)
    {
        // Check if already applied
        var existing = await _context.ApartmentApplications
            .FirstOrDefaultAsync(a => a.UserId == userId && a.ApartmentId == apartmentId);

        if (existing != null) return null; // Already applied

        // Check if apartment exists
        var apartment = await _apartmentService.GetApartmentByIdAsync(apartmentId);
        if (apartment == null)
            throw new ArgumentException("Apartment not found");

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
        if (apartment != null && apartment.LandlordId.HasValue)
        {
             await _notificationHub.Clients.Group(apartment.LandlordId.Value.ToString()).SendAsync("ReceiveNotification", 
                "New Application!", 
                $"You have a new application for '{apartment.Title}'.", 
                "info");
        }

        return application;
    }


    public async Task<List<ApartmentApplicationDto>> GetLandlordApplicationsAsync(int landlordId)
    {
        // Get all apartments owned by landlord
        var apartments = await _apartmentService.GetApartmentsByLandlordIdAsync(landlordId);
        var apartmentIds = apartments.Select(a => a.ApartmentId).ToList();

        // Get applications for landlord's apartments
        var applications = await _context.ApartmentApplications
            .Where(a => a.ApartmentId.HasValue && apartmentIds.Contains(a.ApartmentId.Value))
            .OrderByDescending(a => a.ApplicationDate)
            .ToListAsync();

        // Map to DTOs with apartment and user details
        var applicationDtos = new List<ApartmentApplicationDto>();
        
        foreach (var app in applications)
        {
            var apartment = apartments.FirstOrDefault(a => a.ApartmentId == app.ApartmentId);
            var userProfile = app.UserId.HasValue && app.UserId.Value > 0 
                ? await _userService.GetUserProfileAsync(app.UserId.Value) 
                : null;

            applicationDtos.Add(new ApartmentApplicationDto
            {
                ApplicationId = app.ApplicationId,
                UserId = app.UserId,
                ApartmentId = app.ApartmentId,
                ApplicationDate = app.ApplicationDate,
                Status = app.Status,
                CreatedDate = app.CreatedDate,
                Apartment = apartment != null ? new ApartmentDetailsDto
                {
                    ApartmentId = apartment.ApartmentId,
                    Title = apartment.Title,
                    City = apartment.City,
                    Rent = apartment.Rent
                } : null,
                User = userProfile != null ? new UserDetailsDto
                {
                    UserId = userProfile.UserId,
                    FirstName = userProfile.FirstName,
                    LastName = userProfile.LastName,
                    Email = userProfile.Email
                } : null
            });
        }

        return applicationDtos;
    }


    public async Task<List<ApartmentApplicationDto>> GetTenantApplicationsAsync(int tenantId)
    {
        // Get tenant's applications
        var applications = await _context.ApartmentApplications
            .Where(a => a.UserId == tenantId)
            .OrderByDescending(a => a.ApplicationDate)
            .ToListAsync();

        // Map to DTOs with apartment details
        var applicationDtos = new List<ApartmentApplicationDto>();
        
        foreach (var app in applications)
        {
            var apartment = app.ApartmentId.HasValue 
                ? await _apartmentService.GetApartmentByIdAsync(app.ApartmentId.Value) 
                : null;

            applicationDtos.Add(new ApartmentApplicationDto
            {
                ApplicationId = app.ApplicationId,
                UserId = app.UserId,
                ApartmentId = app.ApartmentId,
                ApplicationDate = app.ApplicationDate,
                Status = app.Status,
                CreatedDate = app.CreatedDate,
                Apartment = apartment != null ? new ApartmentDetailsDto
                {
                    ApartmentId = apartment.ApartmentId,
                    Title = apartment.Title,
                    City = apartment.City,
                    Rent = apartment.Rent
                } : null,
                User = null // Tenant doesn't need their own details
            });
        }

        return applicationDtos;
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

        // Notify Tenant with specific approval/rejection messages
        if (application.UserId.HasValue)
        {
            var apartment = application.ApartmentId.HasValue
                ? await _apartmentService.GetApartmentByIdAsync(application.ApartmentId.Value)
                : null;
            var aptTitle = apartment?.Title ?? "the apartment";

            if (status == "Approved")
            {
                await _notificationHub.Clients.Group(application.UserId.Value.ToString()).SendAsync(
                    "ReceiveNotification",
                    "Application Approved! 🎉",
                    $"Congratulations! Your application for '{aptTitle}' has been approved. You can now schedule a viewing.",
                    "success");
            }
            else if (status == "Rejected")
            {
                var metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    apartmentTitle = aptTitle,
                    apartmentId = application.ApartmentId ?? 0
                });
                await _notificationHub.Clients.Group(application.UserId.Value.ToString()).SendAsync(
                    "ReceiveNotification",
                    "Application Rejected",
                    $"Your application for '{aptTitle}' was not approved.",
                    "rejection",
                    metadata);
            }
            else
            {
                await _notificationHub.Clients.Group(application.UserId.Value.ToString()).SendAsync(
                    "ReceiveNotification",
                    $"Application {status}",
                    $"Your application status has been updated to: {status}.",
                    "info");
            }
        }

        return application;
    }
}
