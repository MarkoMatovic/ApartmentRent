namespace Lander.src.Modules.Listings.Services;

public interface IApartmentNotificationService
{
    Task NotifyNewListingAsync(string title, string city);
    Task NotifyListingRemovedAsync(int apartmentId, string title);

    /// <summary>
    /// Checks all active saved searches and sends an in-app notification to every user
    /// whose saved filters match the newly created listing.
    /// </summary>
    Task NotifyNewListingMatchesAsync(ListingAlertContext listing);

    /// <summary>
    /// Checks all active saved searches and sends an in-app "price dropped" notification
    /// to every user whose saved filters match the updated listing.
    /// </summary>
    Task NotifyPriceDropMatchesAsync(ListingAlertContext listing, decimal oldRent);
}
