using System.Security.Claims;
using Lander.src.Common;
using Lander.src.Infrastructure.Services;
using Lander.src.Notifications.Dtos.InputDto;
using Lander.src.Modules.Communication.Interfaces;
using Lander.src.Modules.ApartmentApplications.Interfaces;
using Lander.src.Modules.ApartmentApplications.Models;
using Lander.src.Modules.ApartmentApplications.Dtos.Dto;
using Lander.src.Modules.Listings.Interfaces;
using Lander.src.Modules.Listings;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Lander;
using Lander.src.Notifications.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.ApartmentApplications.Implementation;

public class ApartmentApplicationService : IApartmentApplicationService
{
    private readonly ApplicationsContext _context;
    private readonly ListingsContext _listingsContext;
    private readonly UsersContext _usersContext;
    private readonly IApartmentService _apartmentService;
    private readonly Lander.src.Notifications.Interfaces.INotificationService _bell;
    private readonly IUserInterface _userService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEmailService _emailService;
    private readonly IPresenceTracker _presence;
    private readonly ILogger<ApartmentApplicationService> _logger;

    public ApartmentApplicationService(
        ApplicationsContext context,
        ListingsContext listingsContext,
        UsersContext usersContext,
        IApartmentService apartmentService,
        Lander.src.Notifications.Interfaces.INotificationService bell,
        IUserInterface userService,
        IHttpContextAccessor httpContextAccessor,
        IEmailService emailService,
        IPresenceTracker presence,
        ILogger<ApartmentApplicationService> logger)
    {
        _context = context;
        _listingsContext = listingsContext;
        _usersContext = usersContext;
        _apartmentService = apartmentService;
        _bell = bell;
        _userService = userService;
        _httpContextAccessor = httpContextAccessor;
        _emailService = emailService;
        _presence = presence;
        _logger = logger;
    }

    /// <summary>
    /// Stores a notification and pushes it live (in the user's language the browser translates the key).
    /// Best-effort: the application itself is already saved and must not fail because of a notification.
    /// </summary>
    private async Task SendBellAsync(
        int recipientUserId, int senderUserId, string key, Dictionary<string, string> parameters,
        string title, string message, string actionTarget, string type = "info", string? metadata = null)
    {
        try
        {
            await _bell.SendNotificationAsync(new CreateNotificationInputDto
            {
                Title = title,
                Message = message,
                ActionType = key,
                ActionTarget = actionTarget,
                Key = key,
                Params = parameters,
                Type = type,
                Metadata = metadata,
                CreatedByGuid = Guid.Empty,
                SenderUserId = senderUserId,
                RecipientUserId = recipientUserId,
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not send notification {Key} to user {UserId}", key, recipientUserId);
        }
    }

    /// <summary>
    /// Tells the landlord, in Serbian and with the applicant's name, that someone applied. Live push and
    /// bell entry always; an e-mail only when the landlord is not on the site (they already see the bell).
    /// Never throws: the application is already saved and must not fail because of a notification.
    /// </summary>
    private async Task NotifyLandlordOfApplicationAsync(int landlordId, int applicantId, int apartmentId, string? apartmentTitle)
    {
        try
        {
            var people = await _usersContext.Users.AsNoTracking()
                .Where(u => u.UserId == applicantId || u.UserId == landlordId)
                .ToDictionaryAsync(u => u.UserId);
            people.TryGetValue(applicantId, out var applicant);
            people.TryGetValue(landlordId, out var landlord);

            var applicantName = applicant is null ? "Neko" : $"{applicant.FirstName} {applicant.LastName}".Trim();
            var title = "Nova prijava za vaš stan";
            var message = $"{applicantName} se prijavio/la za „{apartmentTitle}“.";

            await SendBellAsync(landlordId, applicantId, "application_new",
                new Dictionary<string, string> { ["name"] = applicantName, ["title"] = apartmentTitle ?? string.Empty },
                title, message, "/applications/received");

            if (landlord is not null && !string.IsNullOrEmpty(landlord.Email) && !_presence.IsOnline(landlordId))
                await _emailService.SendNewApplicationEmailAsync(landlord.Email, landlord.FirstName, apartmentTitle ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not notify landlord {LandlordId} about a new application", landlordId);
        }
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
            await NotifyLandlordOfApplicationAsync(apartment.LandlordId.Value, userId, apartmentId, apartment.Title);
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
            var aptTitle = apartment?.Title ?? "stan";

            var byTitle = new Dictionary<string, string> { ["title"] = aptTitle };
            if (status == ApplicationStatuses.Approved)
            {
                var msg = $"Čestitamo! Vaša prijava za „{aptTitle}“ je odobrena. Sada možete da zakažete razgledanje.";
                await SendBellAsync(application.UserId.Value, landlordUserId, "application_approved", byTitle,
                    "Prijava odobrena! 🎉", msg, $"/apartments/{application.ApartmentId}", type: "success");
            }
            else if (status == ApplicationStatuses.Rejected)
            {
                var metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    apartmentTitle = aptTitle,
                    apartmentId = application.ApartmentId ?? 0
                });
                var msg = $"Vaša prijava za „{aptTitle}“ nije odobrena.";
                await SendBellAsync(application.UserId.Value, landlordUserId, "application_rejected", byTitle,
                    "Prijava odbijena", msg, "/applications/sent", type: "rejection", metadata: metadata);
            }
            else
            {
                var label = status == ApplicationStatuses.Pending ? "na čekanju" : status;
                var msg = $"Status vaše prijave je ažuriran: {label}.";
                // Pending and Cancelled have their own translations; any other status shows its name.
                var statusKey = status switch
                {
                    ApplicationStatuses.Pending => "application_pending",
                    ApplicationStatuses.Cancelled => "application_cancelled",
                    _ => "application_status",
                };
                await SendBellAsync(application.UserId.Value, landlordUserId, statusKey,
                    new Dictionary<string, string> { ["title"] = aptTitle, ["status"] = status },
                    $"Prijava: {label}", msg, "/applications/sent");
            }
        }

        return application;
    }
}
