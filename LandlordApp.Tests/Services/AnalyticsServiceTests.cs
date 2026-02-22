using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Lander;
using Lander.src.Modules.Analytics.Implementation;
using Lander.src.Modules.Analytics.Models;
using Lander.src.Modules.Listings.Models;
using Lander.src.Modules.Roommates.Models;
using System.Text.Json;

namespace LandlordApp.Tests.Services;

public class AnalyticsServiceTests : IDisposable
{
    private readonly AnalyticsContext _analyticsContext;
    private readonly ListingsContext _listingsContext;
    private readonly RoommatesContext _roommatesContext;
    private readonly AnalyticsService _analyticsService;

    public AnalyticsServiceTests()
    {
        var analyticsOptions = new DbContextOptionsBuilder<AnalyticsContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var listingsOptions = new DbContextOptionsBuilder<ListingsContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var roommatesOptions = new DbContextOptionsBuilder<RoommatesContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _analyticsContext = new AnalyticsContext(analyticsOptions);
        _listingsContext  = new ListingsContext(listingsOptions);
        _roommatesContext = new RoommatesContext(roommatesOptions);

        _analyticsService = new AnalyticsService(_analyticsContext, _listingsContext, _roommatesContext);
    }

    public void Dispose()
    {
        _analyticsContext.Database.EnsureDeleted();
        _listingsContext.Database.EnsureDeleted();
        _roommatesContext.Database.EnsureDeleted();
        _analyticsContext.Dispose();
        _listingsContext.Dispose();
        _roommatesContext.Dispose();
    }

    // Helper: create an AnalyticsEvent with required fields
    private static AnalyticsEvent Evt(string type, string category = "Cat",
        int? entityId = null, string? query = null, int? userId = null,
        DateTime? date = null) =>
        new AnalyticsEvent
        {
            EventType     = type,
            EventCategory = category,
            EntityId      = entityId,
            SearchQuery   = query,
            UserId        = userId,
            CreatedDate   = date ?? DateTime.UtcNow
        };

    // ─── Basic Tracking & Summary ────────────────────────────────────────────

    [Fact]
    public async Task TrackEventAsync_ShouldSaveDetailedEvent()
    {
        var metadata = new Dictionary<string, string> { { "Source", "Mobile" } };
        await _analyticsService.TrackEventAsync("View", "UI", 1, "Page", metadata: metadata, userId: 123);

        var ev = await _analyticsContext.AnalyticsEvents.FirstAsync();
        ev.EventType.Should().Be("View");
        ev.UserId.Should().Be(123);
        ev.MetadataJson.Should().Contain("Mobile");
    }

    [Fact]
    public async Task GetSummaryAsync_WithDateFilters_ShouldReturnSubset()
    {
        var old  = DateTime.UtcNow.AddDays(-10);
        var recent = DateTime.UtcNow;

        _analyticsContext.AnalyticsEvents.AddRange(
            Evt("ApartmentView", date: old),
            Evt("ApartmentView", date: recent)
        );
        await _analyticsContext.SaveChangesAsync();

        var summary = await _analyticsService.GetSummaryAsync(from: DateTime.UtcNow.AddDays(-5));
        summary.TotalApartmentViews.Should().Be(1);
    }

    [Fact]
    public async Task GetSummaryAsync_NoFilters_ShouldCountAll()
    {
        _analyticsContext.AnalyticsEvents.AddRange(
            Evt("ApartmentView"),
            Evt("RoommateView"),
            Evt("Search")
        );
        await _analyticsContext.SaveChangesAsync();

        var summary = await _analyticsService.GetSummaryAsync();
        summary.TotalEvents.Should().Be(3);
        summary.TotalApartmentViews.Should().Be(1);
        summary.TotalRoommateViews.Should().Be(1);
        summary.TotalSearches.Should().Be(1);
    }

    // ─── Global Reports ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetTopSearchTermsAsync_ShouldRankByFrequency()
    {
        _analyticsContext.AnalyticsEvents.AddRange(
            Evt("Search", query: "Beograd"),
            Evt("Search", query: "Beograd"),
            Evt("Search", query: "Novi Sad")
        );
        await _analyticsContext.SaveChangesAsync();

        var terms = await _analyticsService.GetTopSearchTermsAsync(10);
        terms.Should().HaveCountGreaterThan(1);
        terms[0].SearchTerm.Should().Be("Beograd");
        terms[0].SearchCount.Should().Be(2);
    }

    [Fact]
    public async Task GetEventTrendsAsync_ShouldGroupByDate()
    {
        var today     = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);

        _analyticsContext.AnalyticsEvents.AddRange(
            Evt("Click", date: today),
            Evt("Click", date: today),
            Evt("Click", date: yesterday)
        );
        await _analyticsContext.SaveChangesAsync();

        var trends = await _analyticsService.GetEventTrendsAsync(yesterday, today);
        // GetEventTrendsAsync groups by Date+EventType; with one type "Click" we get 2 groups
        trends.Should().HaveCount(2);
        trends.Sum(t => t.Count).Should().Be(3);
        trends.First(t => t.Date == today).Count.Should().Be(2);
        trends.First(t => t.Date == yesterday).Count.Should().Be(1);
    }

    [Fact]
    public async Task GetEventTrendsAsync_FilterByType_ShouldOnlyReturnMatchingType()
    {
        var today = DateTime.UtcNow.Date;
        _analyticsContext.AnalyticsEvents.AddRange(
            Evt("Click", date: today),
            Evt("View",  date: today)
        );
        await _analyticsContext.SaveChangesAsync();

        var trends = await _analyticsService.GetEventTrendsAsync(today, today, "Click");
        trends.Should().HaveCount(1);
        trends[0].Count.Should().Be(1);
    }

    // ─── User-Specific Analytics ─────────────────────────────────────────────

    [Fact]
    public async Task GetUserRoommateTrendsAsync_ShouldCalculateAverageBudgetPerCity()
    {
        var userId = 1;
        _analyticsContext.AnalyticsEvents.AddRange(
            Evt("RoommateView", entityId: 101, userId: userId),
            Evt("RoommateView", entityId: 102, userId: userId)
        );
        _roommatesContext.Roommates.AddRange(
            new Roommate { RoommateId = 101, PreferredLocation = "Beograd", BudgetMax = 500, IsActive = true },
            new Roommate { RoommateId = 102, PreferredLocation = "Beograd", BudgetMax = 300, IsActive = true }
        );
        await _analyticsContext.SaveChangesAsync();
        await _roommatesContext.SaveChangesAsync();

        var trends = await _analyticsService.GetUserRoommateTrendsAsync(userId);
        trends.AveragePrices.Should().Contain(p => p.City == "Beograd" && p.AveragePrice == 400);
        trends.PopularCities.Should().Contain(c => c.City == "Beograd" && c.ViewCount == 2);
    }

    [Fact]
    public async Task GetUserCompleteAnalyticsAsync_ShouldAggregateAllMetrics()
    {
        var userId = 5;
        _analyticsContext.AnalyticsEvents.AddRange(
            Evt("ApartmentView", category: "A", userId: userId),
            Evt("MessageSent",   category: "B", userId: userId)
        );
        await _analyticsContext.SaveChangesAsync();

        var result = await _analyticsService.GetUserCompleteAnalyticsAsync(userId);
        result.TotalApartmentViews.Should().Be(1);
        result.EventsByCategory.Should().ContainKey("B");
    }

    // ─── Landlord Reports ────────────────────────────────────────────────────

    [Fact]
    public async Task GetLandlordApartmentViewsAsync_ShouldExcludeOtherLandlords()
    {
        var l1 = 10;
        var l2 = 20;
        _listingsContext.Apartments.AddRange(
            new Apartment { ApartmentId = 1, LandlordId = l1, Title = "Apt L1", Address = "Addr 1", Rent = 500, IsActive = true, IsDeleted = false },
            new Apartment { ApartmentId = 2, LandlordId = l2, Title = "Apt L2", Address = "Addr 2", Rent = 600, IsActive = true, IsDeleted = false }
        );
        _analyticsContext.AnalyticsEvents.AddRange(
            Evt("ApartmentView", entityId: 1),
            Evt("ApartmentView", entityId: 2)
        );
        await _listingsContext.SaveChangesAsync();
        await _analyticsContext.SaveChangesAsync();

        var stats = await _analyticsService.GetLandlordApartmentViewsAsync(l1);
        // Returns ALL l1 apartments (with view count), not l2 apartments
        stats.Should().HaveCount(1);
        stats[0].ApartmentId.Should().Be(1);
        stats[0].ViewCount.Should().Be(1);
    }

    [Fact]
    public async Task GetLandlordApartmentViewsAsync_NoApartments_ShouldReturnEmpty()
    {
        var stats = await _analyticsService.GetLandlordApartmentViewsAsync(999);
        stats.Should().BeEmpty();
    }
}
