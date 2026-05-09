using System.Text.Json;
using Lander.src.Modules.Communication.Interfaces;
using Lander.src.Modules.SavedSearches.Models;
using Lander.src.Modules.SavedSearches.Services;
using Lander.src.Notifications.Dtos.InputDto;
using Lander.src.Notifications.Interfaces;
using Lander.src.Notifications.NotificationsHub;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Listings.Services;

public class ApartmentNotificationService : IApartmentNotificationService
{
    private readonly IHubContext<NotificationHub> _notificationHub;
    private readonly IEmailService _emailService;
    private readonly SavedSearchesContext _savedSearchesContext;
    private readonly UsersContext _usersContext;
    private readonly INotificationService _inAppNotifications;
    private readonly ILogger<ApartmentNotificationService> _logger;

    // Minimum gap between two saved-search alerts for the same search.
    // Prevents flooding a user when many listings are created in quick succession.
    private static readonly TimeSpan AlertCooldown = TimeSpan.FromHours(1);

    private static readonly JsonSerializerOptions _jsonOpts = new(JsonSerializerDefaults.Web);

    public ApartmentNotificationService(
        IHubContext<NotificationHub> notificationHub,
        IEmailService emailService,
        SavedSearchesContext savedSearchesContext,
        UsersContext usersContext,
        INotificationService inAppNotifications,
        ILogger<ApartmentNotificationService> logger)
    {
        _notificationHub      = notificationHub;
        _emailService         = emailService;
        _savedSearchesContext = savedSearchesContext;
        _usersContext         = usersContext;
        _inAppNotifications   = inAppNotifications;
        _logger               = logger;
    }

    // ─── Existing methods ────────────────────────────────────────────────────────

    public async Task NotifyNewListingAsync(string title, string city)
    {
        await _notificationHub.Clients.All.SendAsync("ReceiveNotification",
            "New Apartment Listed!",
            $"A new apartment '{title}' is now available in {city}.",
            "success");
    }

    public async Task NotifyListingRemovedAsync(int apartmentId, string title)
    {
        try
        {
            var affectedUserIds = await _savedSearchesContext.SavedSearches
                .AsNoTracking()
                .Where(ss => ss.IsActive &&
                             ss.EmailNotificationsEnabled &&
                             ss.FiltersJson != null &&
                             ss.FiltersJson.Contains($"\"apartmentId\":{apartmentId}"))
                .Select(ss => ss.UserId)
                .Distinct()
                .ToListAsync();

            if (!affectedUserIds.Any()) return;

            var usersToNotify = await _usersContext.Users
                .AsNoTracking()
                .Where(u => affectedUserIds.Contains(u.UserId) && u.IsActive)
                .ToListAsync();

            var emailTasks = usersToNotify.Select(user =>
                _emailService.SendListingUnavailableEmailAsync(
                    user.Email,
                    user.FirstName,
                    title,
                    "This listing has been removed by the landlord."));

            await Task.WhenAll(emailTasks);

            _logger.LogInformation(
                "Sent listing unavailable notifications to {Count} user(s) for apartment {ApartmentId}",
                usersToNotify.Count, apartmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying saved search users for deleted apartment {ApartmentId}", apartmentId);
        }
    }

    // ─── New listing alert ───────────────────────────────────────────────────────

    public async Task NotifyNewListingMatchesAsync(ListingAlertContext listing)
    {
        try
        {
            var matched = await MatchSavedSearchesAsync(listing);
            if (matched.Count == 0) return;

            var now = DateTime.UtcNow;

            foreach (var (search, userId) in matched)
            {
                // Skip if we already notified this search within the cooldown window.
                if (search.LastNotificationSent.HasValue &&
                    now - search.LastNotificationSent.Value < AlertCooldown)
                    continue;

                // Don't notify the landlord about their own listing.
                if (listing.LandlordId.HasValue && userId == listing.LandlordId.Value)
                    continue;

                try
                {
                    await _inAppNotifications.SendNotificationAsync(new CreateNotificationInputDto
                    {
                        Title           = "Novi oglas odgovara vašoj pretrazi",
                        Message         = $"Pronađen je novi oglas \"{listing.Title}\" u {listing.City ?? "vašem gradu"} " +
                                          $"za {listing.Rent:N0} € koji odgovara pretrazi \"{search.Name}\".",
                        ActionType      = "new_listing_match",
                        ActionTarget    = listing.ApartmentId.ToString(),
                        CreatedByGuid   = Guid.Empty,
                        SenderUserId    = 0,
                        RecipientUserId = userId
                    });

                    // Stamp the cooldown so the next tick doesn't re-notify.
                    await _savedSearchesContext.SavedSearches
                        .Where(ss => ss.SavedSearchId == search.SavedSearchId)
                        .ExecuteUpdateAsync(s => s.SetProperty(ss => ss.LastNotificationSent, now));

                    _logger.LogInformation(
                        "New-listing alert sent: SavedSearchId={Id}, UserId={UserId}, ApartmentId={ApartmentId}",
                        search.SavedSearchId, userId, listing.ApartmentId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to send new-listing alert for SavedSearchId={Id}", search.SavedSearchId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error in NotifyNewListingMatchesAsync for ApartmentId={ApartmentId}", listing.ApartmentId);
        }
    }

    // ─── Price drop alert ────────────────────────────────────────────────────────

    public async Task NotifyPriceDropMatchesAsync(ListingAlertContext listing, decimal oldRent)
    {
        try
        {
            var matched = await MatchSavedSearchesAsync(listing);
            if (matched.Count == 0) return;

            decimal dropAmount  = oldRent - listing.Rent;
            decimal dropPercent = oldRent > 0 ? dropAmount / oldRent * 100 : 0;
            var now = DateTime.UtcNow;

            foreach (var (search, userId) in matched)
            {
                // Don't notify the landlord about their own listing.
                if (listing.LandlordId.HasValue && userId == listing.LandlordId.Value)
                    continue;

                try
                {
                    await _inAppNotifications.SendNotificationAsync(new CreateNotificationInputDto
                    {
                        Title           = "Cijena oglasa je smanjena!",
                        Message         = $"Oglas \"{listing.Title}\" iz pretrage \"{search.Name}\" " +
                                          $"je pojeftinio sa {oldRent:N0} € na {listing.Rent:N0} € " +
                                          $"(−{dropAmount:N0} €, {dropPercent:F0}% jeftinije).",
                        ActionType      = "price_drop",
                        ActionTarget    = listing.ApartmentId.ToString(),
                        CreatedByGuid   = Guid.Empty,
                        SenderUserId    = 0,
                        RecipientUserId = userId
                    });

                    await _savedSearchesContext.SavedSearches
                        .Where(ss => ss.SavedSearchId == search.SavedSearchId)
                        .ExecuteUpdateAsync(s => s.SetProperty(ss => ss.LastNotificationSent, now));

                    _logger.LogInformation(
                        "Price-drop alert sent: SavedSearchId={Id}, UserId={UserId}, ApartmentId={ApartmentId}, OldRent={Old}, NewRent={New}",
                        search.SavedSearchId, userId, listing.ApartmentId, oldRent, listing.Rent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to send price-drop alert for SavedSearchId={Id}", search.SavedSearchId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error in NotifyPriceDropMatchesAsync for ApartmentId={ApartmentId}", listing.ApartmentId);
        }
    }

    // ─── Shared matching logic ───────────────────────────────────────────────────

    /// <summary>
    /// Loads all active, notification-enabled saved searches and returns those
    /// whose filters match <paramref name="listing"/>, together with their owner userId.
    /// </summary>
    private async Task<List<(SavedSearch Search, int UserId)>> MatchSavedSearchesAsync(
        ListingAlertContext listing)
    {
        // Load only the searches that have notifications enabled.
        // FiltersJson is loaded client-side for in-memory filter evaluation.
        var activeSavedSearches = await _savedSearchesContext.SavedSearches
            .AsNoTracking()
            .Where(ss => ss.IsActive && ss.EmailNotificationsEnabled)
            .ToListAsync();

        var result = new List<(SavedSearch, int)>();

        foreach (var ss in activeSavedSearches)
        {
            if (string.IsNullOrWhiteSpace(ss.FiltersJson)) continue;

            SavedSearchFilters? filters;
            try
            {
                filters = JsonSerializer.Deserialize<SavedSearchFilters>(ss.FiltersJson, _jsonOpts);
            }
            catch (JsonException)
            {
                _logger.LogWarning("Could not deserialize FiltersJson for SavedSearchId={Id}", ss.SavedSearchId);
                continue;
            }

            if (filters is null) continue;
            if (ApartmentMatchesFilters(listing, filters))
                result.Add((ss, ss.UserId));
        }

        return result;
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="listing"/> satisfies every non-null
    /// filter criterion in <paramref name="filters"/>.
    /// </summary>
    private static bool ApartmentMatchesFilters(ListingAlertContext apt, SavedSearchFilters f)
    {
        if (f.ListingType.HasValue  && apt.ListingType  != f.ListingType.Value)  return false;
        if (f.ApartmentType.HasValue && apt.ApartmentType != f.ApartmentType.Value) return false;

        if (!string.IsNullOrWhiteSpace(f.City) &&
            (apt.City == null ||
             !apt.City.Contains(f.City, StringComparison.OrdinalIgnoreCase)))
            return false;

        if (f.MinRent.HasValue      && apt.Rent          < f.MinRent.Value)      return false;
        if (f.MaxRent.HasValue      && apt.Rent          > f.MaxRent.Value)      return false;
        if (f.NumberOfRooms.HasValue && apt.NumberOfRooms != f.NumberOfRooms.Value) return false;

        // For boolean features: only reject if the FILTER requires true and the listing doesn't have it.
        // A filter value of false/null means "don't care".
        if (f.IsFurnished            == true && !apt.IsFurnished)            return false;
        if (f.IsPetFriendly          == true && !apt.IsPetFriendly)          return false;
        if (f.IsSmokingAllowed       == true && !apt.IsSmokingAllowed)       return false;
        if (f.HasParking             == true && !apt.HasParking)             return false;
        if (f.HasBalcony             == true && !apt.HasBalcony)             return false;
        if (f.IsImmediatelyAvailable == true && !apt.IsImmediatelyAvailable) return false;

        return true;
    }
}
