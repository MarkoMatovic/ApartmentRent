using Lander.src.Modules.Communication.Hubs;
using System.Text.Json;
using FluentAssertions;
using Lander;
using Lander.src.Modules.Communication.Interfaces;
using Lander.src.Modules.Listings.Models;
using Lander.src.Modules.Listings.Services;
using Lander.src.Modules.SavedSearches.Models;
using Lander.src.Notifications.Dtos.InputDto;
using Lander.src.Notifications.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LandlordApp.Tests.Notifications;

/// <summary>
/// The notifications the server sends on its own (a new listing, a saved-search alert, a listing about to
/// expire): each one carries a translation key and its values, and a route the browser opens on click.
/// </summary>
public class NotificationSendersTests
{
    private static DbContextOptions<T> InMemory<T>() where T : DbContext =>
        new DbContextOptionsBuilder<T>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

    // ── the broadcast about a new listing ────────────────────────────────────

    [Fact]
    public async Task NewListing_BroadcastCarriesItsKeyValuesAndTheListingsPage()
    {
        object?[]? sent = null;
        var proxy = new Mock<IClientProxy>();
        proxy.Setup(p => p.SendCoreAsync("ReceiveNotification", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((_, args, _) => sent = args)
            .Returns(Task.CompletedTask);
        var clients = new Mock<IHubClients>();
        clients.Setup(c => c.All).Returns(proxy.Object);
        var hub = new Mock<IHubContext<ChatHub>>();
        hub.Setup(h => h.Clients).Returns(clients.Object);

        var service = new ApartmentNotificationService(
            hub.Object, new Mock<IEmailService>().Object, new SavedSearchesContext(InMemory<SavedSearchesContext>()),
            new UsersContext(InMemory<UsersContext>()), new Mock<INotificationService>().Object,
            NullLogger<ApartmentNotificationService>.Instance);

        await service.NotifyNewListingAsync("Stan na Zvezdari", "Beograd");

        sent.Should().NotBeNull();
        sent![0].Should().Be("Novi stan na sajtu!");
        ((string)sent[1]!).Should().Contain("Stan na Zvezdari").And.Contain("Beograd");
        sent[2].Should().Be("success");
        using var d = JsonDocument.Parse((string)sent[4]!);
        d.RootElement.GetProperty("key").GetString().Should().Be("new_listing");
        d.RootElement.GetProperty("params").GetProperty("title").GetString().Should().Be("Stan na Zvezdari");
        d.RootElement.GetProperty("params").GetProperty("city").GetString().Should().Be("Beograd");
        d.RootElement.GetProperty("link").GetString().Should().Be("/apartments");
    }

    // ── saved-search alerts ──────────────────────────────────────────────────

    private static (ApartmentNotificationService service, List<CreateNotificationInputDto> sent) BuildAlertService(string filtersJson, int userId) =>
        BuildAlertService(new[]
        {
            new SavedSearch
            {
                UserId = userId, Name = "Beograd do 500", SearchType = "Apartment", FiltersJson = filtersJson,
                EmailNotificationsEnabled = true, IsActive = true,
            },
        });

    private static (ApartmentNotificationService service, List<CreateNotificationInputDto> sent) BuildAlertService(IEnumerable<SavedSearch> savedSearches)
    {
        var sent = new List<CreateNotificationInputDto>();
        var notifications = new Mock<INotificationService>();
        notifications.Setup(n => n.SendNotificationAsync(It.IsAny<CreateNotificationInputDto>()))
            .Callback<CreateNotificationInputDto>(dto => sent.Add(dto))
            .ReturnsAsync(new Lander.src.Notifications.Dtos.Dto.NotificationDto());

        var searches = new SavedSearchesContext(InMemory<SavedSearchesContext>());
        searches.SavedSearches.AddRange(savedSearches);
        searches.SaveChanges();

        var hub = new Mock<IHubContext<ChatHub>>();
        var service = new ApartmentNotificationService(
            hub.Object, new Mock<IEmailService>().Object, searches, new UsersContext(InMemory<UsersContext>()),
            notifications.Object, NullLogger<ApartmentNotificationService>.Instance);
        return (service, sent);
    }

    private static ListingAlertContext Listing(decimal rent = 450) => new(
        ApartmentId: 42, Title: "Garsonjera", City: "Beograd", Rent: rent, NumberOfRooms: 1,
        ApartmentType: ApartmentType.Studio, ListingType: ListingType.Rent,
        IsFurnished: false, IsPetFriendly: false, IsSmokingAllowed: false, HasParking: false, HasBalcony: false,
        IsImmediatelyAvailable: true, LandlordId: 99);

    [Fact]
    public async Task NewListingMatchingASavedSearch_NotifiesTheSearcherInTheirLanguageAndOpensTheListing()
    {
        var (service, sent) = BuildAlertService("{\"city\":\"Beograd\"}", userId: 7);

        await service.NotifyNewListingMatchesAsync(Listing());

        var n = sent.Should().ContainSingle().Subject;
        n.RecipientUserId.Should().Be(7);
        n.Key.Should().Be("new_listing_match");
        n.ActionType.Should().Be("new_listing_match");
        n.ActionTarget.Should().Be("/apartments/42");
        n.Params.Should().Contain("title", "Garsonjera").And.Contain("city", "Beograd").And.Contain("search", "Beograd do 500");
        n.Params!["price"].Should().Be("450");
        n.Message.Should().Contain("Garsonjera", "the Serbian text is kept as the fallback");
    }

    [Fact]
    public async Task PriceDrop_CarriesTheOldAndNewRentAndThePercentage()
    {
        var (service, sent) = BuildAlertService("{\"city\":\"Beograd\"}", userId: 7);

        await service.NotifyPriceDropMatchesAsync(Listing(rent: 400), oldRent: 500);

        var n = sent.Should().ContainSingle().Subject;
        n.Key.Should().Be("price_drop");
        n.ActionTarget.Should().Be("/apartments/42");
        n.Params.Should().Contain("oldRent", "500").And.Contain("newRent", "400")
            .And.Contain("drop", "100").And.Contain("percent", "20").And.Contain("search", "Beograd do 500");
    }

    [Fact]
    public async Task TheLandlordIsNotToldAboutTheirOwnListing()
    {
        var (service, sent) = BuildAlertService("{\"city\":\"Beograd\"}", userId: 99);   // 99 is the landlord

        await service.NotifyNewListingMatchesAsync(Listing());

        sent.Should().BeEmpty();
    }

    private static SavedSearch Search(int userId, string city, DateTime? lastNotified = null) => new()
    {
        UserId = userId, Name = $"Pretraga {userId}", SearchType = "Apartment", FiltersJson = $"{{\"city\":\"{city}\"}}",
        EmailNotificationsEnabled = true, IsActive = true, LastNotificationSent = lastNotified,
    };

    [Fact]
    public async Task SavedSearchAlerts_AreReadInBatches_SoMatchesAfterTheFirstHundredsAreFoundToo()
    {
        // 1,200 searches for another city, and matching ones at the start, in the middle and at the end.
        var searches = Enumerable.Range(1000, 1200).Select(id => Search(id, "Niš")).ToList();
        searches.Insert(0, Search(1, "Beograd"));
        searches.Insert(700, Search(2, "Beograd"));
        searches.Add(Search(3, "Beograd"));
        var (service, sent) = BuildAlertService(searches);

        await service.NotifyNewListingMatchesAsync(Listing());

        sent.Select(n => n.RecipientUserId).Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public async Task NewListingAlert_SkipsSearchesStillInTheirCooldown_ButAPriceDropDoesNot()
    {
        var recently = DateTime.UtcNow.AddMinutes(-10);
        var (service, sent) = BuildAlertService(new[] { Search(7, "Beograd", lastNotified: recently), Search(8, "Beograd") });

        await service.NotifyNewListingMatchesAsync(Listing());
        sent.Select(n => n.RecipientUserId).Should().Equal(8);

        sent.Clear();
        await service.NotifyPriceDropMatchesAsync(Listing(rent: 400), oldRent: 500);
        sent.Select(n => n.RecipientUserId).Should().BeEquivalentTo(new[] { 7, 8 });
    }

    [Fact]
    public async Task InactiveSearchesAndSearchesWithoutAlerts_AreNotNotified()
    {
        var inactive = Search(7, "Beograd");
        inactive.IsActive = false;
        var muted = Search(8, "Beograd");
        muted.EmailNotificationsEnabled = false;
        var (service, sent) = BuildAlertService(new[] { inactive, muted, Search(9, "Beograd") });

        await service.NotifyNewListingMatchesAsync(Listing());

        sent.Select(n => n.RecipientUserId).Should().Equal(9);
    }

    // ── a listing about to expire ────────────────────────────────────────────

    [Fact]
    public async Task ExpiryReminder_TellsTheLandlordHowManyDaysAreLeft_AndOpensTheListing()
    {
        var sent = new List<CreateNotificationInputDto>();
        var notifications = new Mock<INotificationService>();
        notifications.Setup(n => n.SendNotificationAsync(It.IsAny<CreateNotificationInputDto>()))
            .Callback<CreateNotificationInputDto>(dto => sent.Add(dto))
            .ReturnsAsync(new Lander.src.Notifications.Dtos.Dto.NotificationDto());

        var db = new ListingsContext(InMemory<ListingsContext>());
        db.Apartments.Add(new Apartment
        {
            ApartmentId = 5, LandlordId = 31, Title = "Stan sa terasom", Address = "Ulica 1", Rent = 400,
            IsActive = true, IsDeleted = false, ListingExpiresAt = DateTime.UtcNow.AddHours(30),
        });
        db.SaveChanges();

        var service = new ListingExpirationService(
            db, notifications.Object, NullLogger<ListingExpirationService>.Instance, new ConfigurationBuilder().Build());

        // the job also stamps ReminderSentAt with a bulk update, which only a relational database supports
        try { await service.RunAsync(); } catch (InvalidOperationException) { }

        var n = sent.Should().ContainSingle().Subject;
        n.RecipientUserId.Should().Be(31);
        n.Key.Should().Be("listing_expiry_reminder");
        n.ActionTarget.Should().Be("/apartments/5");
        n.Params.Should().Contain("title", "Stan sa terasom").And.Contain("days", "2");
    }
}
