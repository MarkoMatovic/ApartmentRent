using Lander.src.Modules.Communication.Interfaces;
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
    private readonly ILogger<ApartmentNotificationService> _logger;

    public ApartmentNotificationService(
        IHubContext<NotificationHub> notificationHub,
        IEmailService emailService,
        SavedSearchesContext savedSearchesContext,
        UsersContext usersContext,
        ILogger<ApartmentNotificationService> logger)
    {
        _notificationHub = notificationHub;
        _emailService = emailService;
        _savedSearchesContext = savedSearchesContext;
        _usersContext = usersContext;
        _logger = logger;
    }

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
            var allSavedSearches = await _savedSearchesContext.SavedSearches
                .Where(ss => ss.IsActive && ss.EmailNotificationsEnabled)
                .ToListAsync();

            var affectedUserIds = allSavedSearches
                .Where(ss => ss.FiltersJson != null &&
                             ss.FiltersJson.Contains($"\"apartmentId\":{apartmentId}",
                                 StringComparison.OrdinalIgnoreCase))
                .Select(ss => ss.UserId)
                .Distinct()
                .ToList();

            if (!affectedUserIds.Any()) return;

            var usersToNotify = await _usersContext.Users
                .Where(u => affectedUserIds.Contains(u.UserId) && u.IsActive)
                .ToListAsync();

            foreach (var user in usersToNotify)
            {
                _ = _emailService.SendListingUnavailableEmailAsync(
                    user.Email,
                    user.FirstName,
                    title,
                    "This listing has been removed by the landlord.");
            }

            _logger.LogInformation(
                "Sent listing unavailable notifications to {Count} user(s) for apartment {ApartmentId}",
                usersToNotify.Count, apartmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying saved search users for deleted apartment {ApartmentId}", apartmentId);
        }
    }
}
