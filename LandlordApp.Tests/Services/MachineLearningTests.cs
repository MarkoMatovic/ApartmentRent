using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Lander;
using Lander.src.Modules.MachineLearning.Implementation;
using Lander.src.Modules.MachineLearning.Dtos;
using Lander.src.Modules.Listings.Models;
using Lander.src.Modules.Roommates.Models;
using Lander.src.Modules.Roommates.Interfaces;

namespace LandlordApp.Tests.Services;

public class MachineLearningTests : IDisposable
{
    private readonly ListingsContext _listingsContext;
    private readonly RoommatesContext _roommatesContext;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly Mock<IRoommateService> _mockRoommateService;
    private readonly string _testTempDir;

    public MachineLearningTests()
    {
        _testTempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testTempDir);

        var listingsOptions = new DbContextOptionsBuilder<ListingsContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var roommatesOptions = new DbContextOptionsBuilder<RoommatesContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _listingsContext = new ListingsContext(listingsOptions);
        _roommatesContext = new RoommatesContext(roommatesOptions);

        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _mockEnvironment.Setup(m => m.ContentRootPath).Returns(_testTempDir);

        _mockRoommateService = new Mock<IRoommateService>();
    }

    public void Dispose()
    {
        _listingsContext.Database.EnsureDeleted();
        _roommatesContext.Database.EnsureDeleted();
        _listingsContext.Dispose();
        _roommatesContext.Dispose();
        if (Directory.Exists(_testTempDir))
            Directory.Delete(_testTempDir, true);
    }

    // Helper: creates a valid Apartment with all required non-nullable fields
    private static Apartment MakeApartment(int id, decimal rent, int? size, bool isActive = true) =>
        new Apartment
        {
            ApartmentId      = id,
            Title            = $"Apt {id}",
            Address          = $"Ulica {id}",
            Rent             = rent,
            SizeSquareMeters = size,
            IsActive         = isActive,
            ListingType      = ListingType.Rent,
            ApartmentType    = ApartmentType.Studio
        };

    // ─── Price Prediction (Success & Training) ───────────────────────────────

    [Fact]
    public async Task TrainModelAsync_SufficientData_ShouldGenerateMetricsAndModel()
    {
        var service = new PricePredictionService(_listingsContext, _mockEnvironment.Object);
        for (int i = 1; i <= 12; i++)
            _listingsContext.Apartments.Add(MakeApartment(i, 500 + (i * 50), 30 + i));
        await _listingsContext.SaveChangesAsync();

        var metrics = await service.TrainModelAsync();

        metrics.Should().NotBeNull();
        metrics.TrainingSampleCount.Should().Be(12);
        service.IsModelTrained().Should().BeTrue();

        var metricsPath = Path.Combine(_testTempDir, "MLModels", "price-prediction-metrics.json");
        File.Exists(metricsPath).Should().BeTrue();
    }

    [Fact]
    public async Task PredictPriceAsync_TrainedModel_ShouldReturnSuccessMessage()
    {
        var service = new PricePredictionService(_listingsContext, _mockEnvironment.Object);
        // Use varied data so the model can fit meaningfully
        for (int i = 1; i <= 12; i++)
            _listingsContext.Apartments.Add(MakeApartment(i, 500 + (i * 50m), 30 + i));
        await _listingsContext.SaveChangesAsync();
        await service.TrainModelAsync();

        var request = new PricePredictionRequestDto { SizeSquareMeters = 55, City = "Beograd" };
        var result = await service.PredictPriceAsync(request);

        // Model is trained; message should confirm success regardless of numeric accuracy
        result.Message.Should().Be("Price prediction successful");
        result.PredictedPrice.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetModelMetricsAsync_Empty_ShouldReturnZeroedMetrics()
    {
        var service = new PricePredictionService(_listingsContext, _mockEnvironment.Object);
        var metrics = await service.GetModelMetricsAsync();
        metrics.RSquared.Should().Be(0);
        metrics.TrainingSampleCount.Should().Be(0);
    }

    // ─── Price Prediction (Error Scenarios) ──────────────────────────────────

    [Fact]
    public async Task PredictPriceAsync_ModelNotTrained_ShouldReturnGracefulMessage()
    {
        var service = new PricePredictionService(_listingsContext, _mockEnvironment.Object);
        var result = await service.PredictPriceAsync(new PricePredictionRequestDto());
        result.Message.Should().Contain("Model not trained yet");
    }

    [Fact]
    public async Task TrainModelAsync_InsufficientData_ShouldThrow()
    {
        var service = new PricePredictionService(_listingsContext, _mockEnvironment.Object);
        _listingsContext.Apartments.Add(MakeApartment(1, 100, 10));
        await _listingsContext.SaveChangesAsync();

        var act = async () => await service.TrainModelAsync();
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ─── Roommate Matching ────────────────────────────────────────────────────

    [Fact]
    public async Task CalculateMatchScoreAsync_OppositeLifestyles_ShouldReturnLowerScore()
    {
        var service = new RoommateMatchingService(_roommatesContext, _mockRoommateService.Object);
        _roommatesContext.Roommates.Add(new Roommate { UserId = 1, Lifestyle = "Party", IsActive = true });
        _roommatesContext.Roommates.Add(new Roommate { UserId = 2, Lifestyle = "Quiet", IsActive = true });
        await _roommatesContext.SaveChangesAsync();

        var score = await service.CalculateMatchScoreAsync(1, 2);
        score.Should().BeLessThan(100);
    }

    [Fact]
    public async Task GetMatchesForUserAsync_ExcludesInactive()
    {
        var service = new RoommateMatchingService(_roommatesContext, _mockRoommateService.Object);
        _roommatesContext.Roommates.Add(new Roommate { UserId = 1, RoommateId = 1, IsActive = true });
        _roommatesContext.Roommates.Add(new Roommate { UserId = 2, RoommateId = 2, IsActive = false });
        await _roommatesContext.SaveChangesAsync();

        var matches = await service.GetMatchesForUserAsync(1);
        matches.Should().BeEmpty();
    }
}
