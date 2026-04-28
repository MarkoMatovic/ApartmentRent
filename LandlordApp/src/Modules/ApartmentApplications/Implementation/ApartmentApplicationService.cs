using System.Security.Claims;
using Lander.src.Common;
using Lander.src.Modules.ApartmentApplications.Interfaces;
using Lander.src.Modules.ApartmentApplications.Models;
using Lander.src.Modules.ApartmentApplications.Dtos.Dto;
using Lander.src.Modules.Listings.Interfaces;
using Lander.src.Modules.Listings;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Lander;
using Lander.src.Notifications.Interfaces;
using Lander.src.Notifications.NotificationsHub;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.ApartmentApplications.Implementation;

public class ApartmentApplicationService : IApartmentApplicationService
{
    private readonly ApplicationsContext _context;
    private readonly ListingsContext _listingsContext;
    private readonly UsersContext _usersContext;
    private readonly IApartmentService _apartmentService;
    private readonly IHubContext<NotificationHub> _notificationHub;
    private readonly IUserInterface _userService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApartmentApplicationService(
        ApplicationsContext context,
        ListingsContext listingsContext,
        UsersContext usersContext,
        IApartmentService apartmentService,
        IHubContext<NotificationHub> notificationHub,
        IUserInterface userService,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _listingsContext = listingsContext;
        _usersContext = usersContext;
        _apartmentService = apartmentService;
        _notificationHub = notificationHub;
        _userService = userService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ApartmentApplication?> ApplyForApartmentAsync(int userId, int apartmentId, bool isPriority = false)
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
            Status = ApplicationStatuses.Pending,
            IsPriority = isPriority,
            CreatedDate = DateTime.UtcNow,
            CreatedByGuid = Guid.TryParse(
                _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub"),
                out var callerGuid) ? callerGuid : Guid.Empty,
        };

        _context.ApartmentApplications.Add(application);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            // Unique constraint (UserId, ApartmentId) — concurrent duplicate request
            return null;
        }
        
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
        // Single query: all apartment IDs owned by landlord
        var apartmentIds = await _listingsContext.Apartments
            .AsNoTracking()
            .Where(a => a.LandlordId == landlordId && !a.IsDeleted)
            .Select(a => a.ApartmentId)
            .ToListAsync();

        var applications = await _context.ApartmentApplications
            .AsNoTracking()
            .Where(a => a.ApartmentId.HasValue && apartmentIds.Contains(a.ApartmentId.Value))
            .OrderByDescending(a => a.IsPriority)
            .ThenByDescending(a => a.ApplicationDate)
            .ToListAsync();

        // Batch fetch apartments and applicant users — no N+1
        var apartments = await _listingsContext.Apartments
            .AsNoTracking()
            .Where(a => apartmentIds.Contains(a.ApartmentId))
            .ToDictionaryAsync(a => a.ApartmentId);

        var userIds = applications
            .Where(a => a.UserId.HasValue && a.UserId.Value > 0)
            .Select(a => a.UserId!.Value)
            .Distinct()
            .ToList();

        var users = await _usersContext.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.UserId))
            .ToDictionaryAsync(u => u.UserId);

        return applications.Select(app =>
        {
            apartments.TryGetValue(app.ApartmentId ?? 0, out var apt);
            users.TryGetValue(app.UserId ?? 0, out var usr);
            return new ApartmentApplicationDto
            {
                ApplicationId = app.ApplicationId,
                UserId = app.UserId,
                ApartmentId = app.ApartmentId,
                ApplicationDate = app.ApplicationDate,
                Status = app.Status,
                IsPriority = app.IsPriority,
                CreatedDate = app.CreatedDate,
                Apartment = apt != null ? new ApartmentDetailsDto
                {
                    ApartmentId = apt.ApartmentId,
                    Title = apt.Title,
                    City = apt.City,
                    Rent = apt.Rent
                } : null,
                User = usr != null ? new UserDetailsDto
                {
                    UserId = usr.UserId,
                    FirstName = usr.FirstName,
                    LastName = usr.LastName,
                    Email = usr.Email
                } : null
            };
        }).ToList();
    }


    public async Task<List<ApartmentApplicationDto>> GetTenantApplicationsAsync(int tenantId)
    {
        var applications = await _context.ApartmentApplications
            .AsNoTracking()
            .Where(a => a.UserId == tenantId)
            .OrderByDescending(a => a.IsPriority)
            .ThenByDescending(a => a.ApplicationDate)
            .ToListAsync();

        // Batch fetch all referenced apartments in one query
        var apartmentIds = applications
            .Where(a => a.ApartmentId.HasValue)
            .Select(a => a.ApartmentId!.Value)
            .Distinct()
            .ToList();

        var apartments = await _listingsContext.Apartments
            .AsNoTracking()
            .Where(a => apartmentIds.Contains(a.ApartmentId))
            .ToDictionaryAsync(a => a.ApartmentId);

        return applications.Select(app =>
        {
            apartments.TryGetValue(app.ApartmentId ?? 0, out var apt);
            return new ApartmentApplicationDto
            {
                ApplicationId = app.ApplicationId,
                UserId = app.UserId,
                ApartmentId = app.ApartmentId,
                ApplicationDate = app.ApplicationDate,
                Status = app.Status,
                CreatedDate = app.CreatedDate,
                IsPriority = app.IsPriority,
                Apartment = apt != null ? new ApartmentDetailsDto
                {
                    ApartmentId = apt.ApartmentId,
                    Title = apt.Title,
                    City = apt.City,
                    Rent = apt.Rent
                } : null,
                User = null
            };
        }).ToList();
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

            if (status == ApplicationStatuses.Approved)
            {
                await _notificationHub.Clients.Group(application.UserId.Value.ToString()).SendAsync(
                    "ReceiveNotification",
                    "Application Approved! 🎉",
                    $"Congratulations! Your application for '{aptTitle}' has been approved. You can now schedule a viewing.",
                    "success");
            }
            else if (status == ApplicationStatuses.Rejected)
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
