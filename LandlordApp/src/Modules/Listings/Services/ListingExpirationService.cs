using Lander.src.Notifications.Dtos.InputDto;
using Lander.src.Notifications.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Listings.Services;

/// <summary>
/// Nightly background service that manages listing lifecycle:
///   • 2 days before expiry  → sends an in-app reminder notification to the landlord.
///   • On expiry day         → sets IsActive = false (listing hidden from search).
///   • After grace period    → sets IsDeleted = true (soft-deleted, awaiting cleanup).
///
/// Config keys (appsettings.json → "Listings"):
///   ExpirationDays          – how long a listing stays active after creation  (default 30)
///   GraceDays               – days after expiry before hard soft-delete        (default 7)
///   ReminderDaysBeforeExpiry– days before expiry to fire the reminder          (default 2)
///   CheckHourUtc            – UTC hour at which the job runs daily             (default 2)
/// </summary>
public sealed class ListingExpirationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ListingExpirationService> _logger;
    private readonly IConfiguration _configuration;

    public ListingExpirationService(
        IServiceScopeFactory scopeFactory,
        ILogger<ListingExpirationService> logger,
        IConfiguration configuration)
    {
        _scopeFactory   = scopeFactory;
        _logger         = logger;
        _configuration  = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ListingExpirationService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await WaitUntilNextRunAsync(stoppingToken);
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                await RunExpirationCheckAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "ListingExpirationService encountered an error during the daily check.");
            }
        }

        _logger.LogInformation("ListingExpirationService stopped.");
    }

    // ─── Core logic ─────────────────────────────────────────────────────────────

    private async Task RunExpirationCheckAsync(CancellationToken ct)
    {
        int graceDays    = _configuration.GetValue<int>("Listings:GraceDays",               7);
        int reminderDays = _configuration.GetValue<int>("Listings:ReminderDaysBeforeExpiry", 2);

        var now = DateTime.UtcNow;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db          = scope.ServiceProvider.GetRequiredService<ListingsContext>();
        var notifService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        // ── 1. Send reminder notifications ──────────────────────────────────────
        //    Target: active listings that expire within `reminderDays` and haven't
        //    been notified yet (ReminderSentAt IS NULL).
        var reminderCutoff = now.AddDays(reminderDays);

        var toRemind = await db.Apartments
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
            .ToListAsync(ct);

        foreach (var apt in toRemind)
        {
            var daysLeft = (int)Math.Ceiling((apt.ListingExpiresAt!.Value - now).TotalDays);
            try
            {
                await notifService.SendNotificationAsync(new CreateNotificationInputDto
                {
                    Title           = "Oglas uskoro ističe",
                    Message         = $"Vaš oglas \"{apt.Title}\" ističe za {daysLeft} dan(a). Obnovite ga kako bi ostao vidljiv.",
                    ActionType      = "listing_expiry_reminder",
                    ActionTarget    = apt.ApartmentId.ToString(),
                    CreatedByGuid   = Guid.Empty,
                    SenderUserId    = apt.LandlordId!.Value,
                    RecipientUserId = apt.LandlordId.Value
                });

                // Mark as notified so we don't spam on subsequent ticks
                await db.Apartments
                    .IgnoreQueryFilters()
                    .Where(a => a.ApartmentId == apt.ApartmentId)
                    .ExecuteUpdateAsync(s => s.SetProperty(a => a.ReminderSentAt, now), ct);

                _logger.LogInformation(
                    "Listing expiry reminder sent: ApartmentId={ApartmentId}, LandlordId={LandlordId}, DaysLeft={Days}",
                    apt.ApartmentId, apt.LandlordId, daysLeft);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send expiry reminder for ApartmentId={ApartmentId}.", apt.ApartmentId);
            }
        }

        // ── 2. Deactivate expired listings ──────────────────────────────────────
        //    Target: active listings whose expiry timestamp is in the past.
        int deactivated = await db.Apartments
            .IgnoreQueryFilters()
            .Where(a =>
                !a.IsDeleted &&
                a.IsActive &&
                a.ListingExpiresAt.HasValue &&
                a.ListingExpiresAt.Value <= now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.IsActive, false)
                .SetProperty(a => a.ModifiedDate, now),
                ct);

        if (deactivated > 0)
            _logger.LogInformation("Deactivated {Count} expired listing(s).", deactivated);

        // ── 3. Soft-delete listings that have been inactive past the grace period ─
        //    Target: deactivated listings whose expiry + grace period has passed.
        var graceDeadline = now.AddDays(-graceDays);

        int softDeleted = await db.Apartments
            .IgnoreQueryFilters()
            .Where(a =>
                !a.IsDeleted &&
                !a.IsActive &&
                a.ListingExpiresAt.HasValue &&
                a.ListingExpiresAt.Value <= graceDeadline)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.IsDeleted, true)
                .SetProperty(a => a.ModifiedDate, now),
                ct);

        if (softDeleted > 0)
            _logger.LogInformation("Soft-deleted {Count} listing(s) past grace period.", softDeleted);
    }

    // ─── Scheduling ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Sleeps until the configured UTC hour today (or tomorrow if that hour has already passed).
    /// </summary>
    private async Task WaitUntilNextRunAsync(CancellationToken ct)
    {
        int targetHour = _configuration.GetValue<int>("Listings:CheckHourUtc", 2);

        var now      = DateTime.UtcNow;
        var nextRun  = new DateTime(now.Year, now.Month, now.Day, targetHour, 0, 0, DateTimeKind.Utc);
        if (nextRun <= now)
            nextRun = nextRun.AddDays(1);

        var delay = nextRun - now;
        _logger.LogDebug("ListingExpirationService next run in {Minutes:F0} min (at {NextRun:u}).",
            delay.TotalMinutes, nextRun);

        await Task.Delay(delay, ct);
    }
}
