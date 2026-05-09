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
            // Filter in SQL — avoid loading the entire SavedSearches table into memory.
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
}
