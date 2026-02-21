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
        _listingsContext = new ListingsContext(listingsOptions);
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

    [Fact]
    public async Task TrackEventAsync_ShouldSaveEvent()
    {
        // Arrange
        var eventType = "ApartmentView";
        var category = "Engagement";
        var entityId = 1;
        var entityType = "Apartment";

        // Act
        await _analyticsService.TrackEventAsync(eventType, category, entityId, entityType);

        // Assert
        var savedEvent = await _analyticsContext.AnalyticsEvents.FirstOrDefaultAsync();
        savedEvent.Should().NotBeNull();
        savedEvent!.EventType.Should().Be(eventType);
        savedEvent.EventCategory.Should().Be(category);
        savedEvent.EntityId.Should().Be(entityId);
        savedEvent.EntityType.Should().Be(entityType);
    }

    [Fact]
    public async Task GetSummaryAsync_ShouldReturnCorrectCounts()
    {
        // Arrange
        _analyticsContext.AnalyticsEvents.AddRange(
            new AnalyticsEvent { EventType = "ApartmentView", EventCategory = "A", CreatedDate = DateTime.UtcNow },
            new AnalyticsEvent { EventType = "RoommateView", EventCategory = "B", CreatedDate = DateTime.UtcNow },
            new AnalyticsEvent { EventType = "SearchApartment", EventCategory = "C", CreatedDate = DateTime.UtcNow },
            new AnalyticsEvent { EventType = "ContactClick", EventCategory = "A", CreatedDate = DateTime.UtcNow }
        );
        await _analyticsContext.SaveChangesAsync();

        // Act
        var summary = await _analyticsService.GetSummaryAsync();

        // Assert
        summary.TotalEvents.Should().Be(4);
        summary.TotalApartmentViews.Should().Be(1);
        summary.TotalRoommateViews.Should().Be(1);
        summary.TotalSearches.Should().Be(1);
        summary.TotalContactClicks.Should().Be(1);
        summary.EventsByCategory.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetSummaryAsync_NoEvents_ShouldReturnEmptySummary()
    {
        // Arrange - Ensure DB is empty (Dispose/Constructor handles it, but let's be sure for this test instance)
        _analyticsContext.AnalyticsEvents.RemoveRange(_analyticsContext.AnalyticsEvents);
        await _analyticsContext.SaveChangesAsync();

        // Act
        var summary = await _analyticsService.GetSummaryAsync();

        // Assert
        summary.TotalEvents.Should().Be(0);
        summary.TotalApartmentViews.Should().Be(0);
        summary.EventsByCategory.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTopViewedApartmentsAsync_ShouldReturnTopApartments()
    {
        // Arrange
        var apt1Id = 1;
        var apt2Id = 2;

        _listingsContext.Apartments.AddRange(
            new Apartment { ApartmentId = apt1Id, Title = "Apt 1", Address = "Test Address 1", IsActive = true, IsDeleted = false },
            new Apartment { ApartmentId = apt2Id, Title = "Apt 2", Address = "Test Address 2", IsActive = true, IsDeleted = false }
        );
        await _listingsContext.SaveChangesAsync();

        _analyticsContext.AnalyticsEvents.AddRange(
            new AnalyticsEvent { EventType = "ApartmentView", EventCategory = "Engagement", EntityId = apt1Id, EntityType = "Apartment", CreatedDate = DateTime.UtcNow },
            new AnalyticsEvent { EventType = "ApartmentView", EventCategory = "Engagement", EntityId = apt1Id, EntityType = "Apartment", CreatedDate = DateTime.UtcNow },
            new AnalyticsEvent { EventType = "ApartmentView", EventCategory = "Engagement", EntityId = apt2Id, EntityType = "Apartment", CreatedDate = DateTime.UtcNow }
        );
        await _analyticsContext.SaveChangesAsync();

        // Act
        var topApartments = await _analyticsService.GetTopViewedApartmentsAsync(10);

        // Assert
        topApartments.Should().HaveCount(2);
        topApartments[0].EntityId.Should().Be(apt1Id);
        topApartments[0].ViewCount.Should().Be(2);
        topApartments[1].EntityId.Should().Be(apt2Id);
        topApartments[1].ViewCount.Should().Be(1);
    }

    [Fact]
    public async Task GetLandlordApartmentViewsAsync_ShouldReturnStatsForLandlord()
    {
        // Arrange
        var landlordId = 10;
        var aptId = 1;

        _listingsContext.Apartments.Add(new Apartment 
        { 
            ApartmentId = aptId, 
            LandlordId = landlordId, 
            Title = "Test Apt", 
            Address = "Test Address",
            IsActive = true, 
            IsDeleted = false 
        });
        await _listingsContext.SaveChangesAsync();

        _analyticsContext.AnalyticsEvents.Add(new AnalyticsEvent 
        { 
            EventType = "ApartmentView", 
            EventCategory = "Engagement",
            EntityId = aptId, 
            EntityType = "Apartment", 
            CreatedDate = DateTime.UtcNow 
        });
        await _analyticsContext.SaveChangesAsync();

        // Act
        var stats = await _analyticsService.GetLandlordApartmentViewsAsync(landlordId);

        // Assert
        stats.Should().HaveCount(1);
        stats[0].ApartmentId.Should().Be(aptId);
        stats[0].ViewCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSummaryAsync_FilterByDate_ShouldReturnCorrectSubset()
    {
        // Arrange
        var oldDate = DateTime.UtcNow.AddDays(-10);
        var newDate = DateTime.UtcNow;

        _analyticsContext.AnalyticsEvents.AddRange(
            new AnalyticsEvent { EventType = "ApartmentView", EventCategory = "A", CreatedDate = oldDate },
            new AnalyticsEvent { EventType = "ApartmentView", EventCategory = "A", CreatedDate = newDate }
        );
        await _analyticsContext.SaveChangesAsync();

        // Act - Only last 5 days
        var summary = await _analyticsService.GetSummaryAsync(DateTime.UtcNow.AddDays(-5));

        // Assert
        summary.TotalEvents.Should().Be(1);
        summary.TotalApartmentViews.Should().Be(1);
    }
}
