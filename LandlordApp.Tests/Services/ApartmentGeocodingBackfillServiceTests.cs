using FluentAssertions;
using Lander;
using Lander.src.Modules.Listings.Interfaces;
using Lander.src.Modules.Listings.Models;
using Lander.src.Modules.Listings.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace LandlordApp.Tests.Services;

public class ApartmentGeocodingBackfillServiceTests : IDisposable
{
    private readonly ListingsContext _db;
    private readonly Mock<IGeocodingService> _geocoder = new();

    public ApartmentGeocodingBackfillServiceTests()
    {
        _db = new ListingsContext(new DbContextOptionsBuilder<ListingsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    public void Dispose() => _db.Dispose();

    private ApartmentGeocodingBackfillService CreateService(int batchSize = 25) => new(
        _db,
        _geocoder.Object,
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Geocoding:BackfillBatchSize"] = batchSize.ToString() })
            .Build(),
        new Mock<ILogger<ApartmentGeocodingBackfillService>>().Object);

    private async Task<Apartment> Add(string address, decimal? lat = null, decimal? lng = null, bool active = true, bool deleted = false)
    {
        var apartment = new Apartment
        {
            Title = address, Address = address, City = "Beograd", Rent = 400,
            Latitude = lat, Longitude = lng, IsActive = active, IsDeleted = deleted,
        };
        _db.Apartments.Add(apartment);
        await _db.SaveChangesAsync();
        return apartment;
    }

    [Fact]
    public async Task Listings_without_a_map_receive_coordinates()
    {
        var apartment = await Add("Valjevska");
        _geocoder.Setup(g => g.GeocodeAsync("Valjevska", "Beograd", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeoPoint(44.8m, 20.4m));

        var updated = await CreateService().RunAsync();

        updated.Should().Be(1);
        var saved = await _db.Apartments.SingleAsync(a => a.ApartmentId == apartment.ApartmentId);
        (saved.Latitude, saved.Longitude).Should().Be((44.8m, 20.4m));
    }

    [Fact]
    public async Task Listings_that_already_have_a_map_are_left_alone()
    {
        await Add("Sa pinom", 45m, 19m);

        var updated = await CreateService().RunAsync();

        updated.Should().Be(0);
        _geocoder.Verify(g => g.GeocodeAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Inactive_and_deleted_listings_are_skipped()
    {
        await Add("Neaktivan", active: false);
        await Add("Obrisan", deleted: true);

        await CreateService().RunAsync();

        _geocoder.Verify(g => g.GeocodeAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Addresses_that_cannot_be_resolved_stay_without_a_map()
    {
        var apartment = await Add("Nepoznata");
        _geocoder.Setup(g => g.GeocodeAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync((GeoPoint?)null);

        var updated = await CreateService().RunAsync();

        updated.Should().Be(0);
        (await _db.Apartments.SingleAsync(a => a.ApartmentId == apartment.ApartmentId)).Latitude.Should().BeNull();
    }

    [Fact]
    public async Task One_run_handles_at_most_one_batch_to_respect_the_provider_limit()
    {
        for (var i = 1; i <= 5; i++) await Add($"Ulica {i}");
        _geocoder.Setup(g => g.GeocodeAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(new GeoPoint(44m, 20m));

        var updated = await CreateService(batchSize: 2).RunAsync();

        updated.Should().Be(2);
        (await _db.Apartments.CountAsync(a => a.Latitude == null)).Should().Be(3);
    }
}
