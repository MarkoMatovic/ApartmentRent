using Lander.src.Notifications.Dtos.InputDto;
using Lander.src.Notifications.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Listings.Services;

/// <summary>
/// Hangfire recurring job (daily 02:00 UTC) that manages listing lifecycle:
///   • 2 days before expiry  → sends an in-app reminder notification to the landlord.
///   • On expiry day         → sets IsActive = false (listing hidden from search).
///   • After grace period    → sets IsDeleted = true (soft-deleted, awaiting cleanup).
///
/// Config keys (appsettings.json → "Listings"):
///   ExpirationDays          – how long a listing stays active after creation  (default 30)
///   GraceDays               – days after expiry before hard soft-delete        (default 7)
///   ReminderDaysBeforeExpiry– days before expiry to fire the reminder          (default 2)
/// </summary>
public sealed class ListingExpirationService
{
    private readonly ListingsContext _db;
    private readonly INotificationService _notifService;
    private readonly ILogger<ListingExpirationService> _logger;
    private readonly IConfiguration _configuration;

    public ListingExpirationService(
        ListingsContext db,
        INotificationService notifService,
        ILogger<ListingExpirationService> logger,
        IConfiguration configuration)
    {
        _db            = db;
        _notifService  = notifService;
        _logger        = logger;
        _configuration = configuration;
    }

    public async Task RunAsync()
    {
        int graceDays    = _configuration.GetValue<int>("Listings:GraceDays",               7);
        int reminderDays = _configuration.GetValue<int>("Listings:ReminderDaysBeforeExpiry", 2);

        var now = DateTime.UtcNow;

        // ── 1. Send reminder notifications ──────────────────────────────────────
        var reminderCutoff = now.AddDays(reminderDays);

        var toRemind = await _db.Apartments
            .IgnoreQueryFilters()
            .Where(a =>
                !a.IsDeleted &&
                a.IsActive &&
                a.LandlordId.HasValue &&
                a.ListingExpiresAt.HasValue &&
                a.ListingExpiresAt.Value <= reminderCutoff &&
                a.ListingExpiresAt.Value > now &&
                a.ReminderSentAt == null)
            .Select(a => new { a.ApartmentId, a.LandlordId, a.Title, a.ListingExpiresAt })
            .ToListAsync();

        foreach (var apt in toRemind)
        {
            var daysLeft = (int)Math.Ceiling((apt.ListingExpiresAt!.Value - now).TotalDays);
            try
            {
                await _notifService.SendNotificationAsync(new CreateNotificationInputDto
                {
                    Title           = "Oglas uskoro ističe",
                    Message         = $"Vaš oglas \"{apt.Title}\" ističe za {daysLeft} dan(a). Obnovite ga kako bi ostao vidljiv.",
                    ActionType      = "listing_expiry_reminder",
                    ActionTarget    = apt.ApartmentId.ToString(),
                    CreatedByGuid   = Guid.Empty,
                    SenderUserId    = apt.LandlordId!.Value,
                    RecipientUserId = apt.LandlordId.Value
                });

                await _db.Apartments
                    .IgnoreQueryFilters()
                    .Where(a => a.ApartmentId == apt.ApartmentId)
                    .ExecuteUpdateAsync(s => s.SetProperty(a => a.ReminderSentAt, now));

                _logger.LogInformation(
                    "Listing expiry reminder sent: ApartmentId={ApartmentId}, LandlordId={LandlordId}, DaysLeft={Days}",
                    apt.ApartmentId, apt.LandlordId, daysLeft);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send expiry reminder for ApartmentId={ApartmentId}.", apt.ApartmentId);
            }
        }

        // ── 2. Deactivate expired listings ──────────────────────────────────────
        int deactivated = await _db.Apartments
            .IgnoreQueryFilters()
            .Where(a =>
                !a.IsDeleted &&
                a.IsActive &&
                a.ListingExpiresAt.HasValue &&
                a.ListingExpiresAt.Value <= now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.IsActive, false)
                .SetProperty(a => a.ModifiedDate, now));

        if (deactivated > 0)
            _logger.LogInformation("Deactivated {Count} expired listing(s).", deactivated);

        // ── 3. Soft-delete listings past grace period ───────────────────────────
        var graceDeadline = now.AddDays(-graceDays);

        int softDeleted = await _db.Apartments
            .IgnoreQueryFilters()
            .Where(a =>
                !a.IsDeleted &&
                !a.IsActive &&
                a.ListingExpiresAt.HasValue &&
                a.ListingExpiresAt.Value <= graceDeadline)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.IsDeleted, true)
                .SetProperty(a => a.ModifiedDate, now));

        if (softDeleted > 0)
            _logger.LogInformation("Soft-deleted {Count} listing(s) past grace period.", softDeleted);

        // ── 4. Deactivate expired featured promotions ───────────────────────────
        int featuredExpired = await _db.Apartments
            .IgnoreQueryFilters()
            .Where(a =>
                !a.IsDeleted &&
                a.IsFeatured &&
                a.FeaturedUntil.HasValue &&
                a.FeaturedUntil.Value <= now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.IsFeatured, false)
                .SetProperty(a => a.FeaturedUntil, (DateTime?)null)
                .SetProperty(a => a.ModifiedDate, now));

        if (featuredExpired > 0)
            _logger.LogInformation("Cleared {Count} expired featured listing promotion(s).", featuredExpired);
    }
}
