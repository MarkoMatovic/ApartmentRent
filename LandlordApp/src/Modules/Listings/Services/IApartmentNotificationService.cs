namespace Lander.src.Modules.Listings.Services;

public interface IApartmentNotificationService
{
    Task NotifyNewListingAsync(string title, string city);
    Task NotifyListingRemovedAsync(int apartmentId, string title);
}
