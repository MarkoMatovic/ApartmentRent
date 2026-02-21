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
        {
            Directory.Delete(_testTempDir, true);
        }
    }

    [Fact]
    public async Task PredictPriceAsync_ModelNotTrained_ShouldReturnMessage()
    {
        // Arrange
        var service = new PricePredictionService(_listingsContext, _mockEnvironment.Object);
        var request = new PricePredictionRequestDto { SizeSquareMeters = 50, NumberOfRooms = 2 };

        // Act
        var result = await service.PredictPriceAsync(request);

        // Assert
        result.Message.Should().Contain("Model not trained yet");
        result.PredictedPrice.Should().Be(0);
    }

    [Fact]
    public async Task TrainModelAsync_InsufficientData_ShouldThrowException()
    {
        // Arrange
        var service = new PricePredictionService(_listingsContext, _mockEnvironment.Object);
        // Only seed 1 apartment, need 10
        _listingsContext.Apartments.Add(new Apartment 
        { 
            Title = "Test Apartment", 
            Address = "Test Address",
            Rent = 1000, 
            SizeSquareMeters = 50, 
            IsActive = true 
        });
        await _listingsContext.SaveChangesAsync();

        // Act
        var act = async () => await service.TrainModelAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Insufficient data*");
    }

    [Fact]
    public async Task CalculateMatchScoreAsync_ValidUsers_ShouldReturnScore()
    {
        // Arrange
        var service = new RoommateMatchingService(_roommatesContext, _mockRoommateService.Object);
        
        var r1 = new Roommate { UserId = 1, BudgetMin = 100, BudgetMax = 500, Lifestyle = "Quiet", IsActive = true };
        var r2 = new Roommate { UserId = 2, BudgetMin = 100, BudgetMax = 500, Lifestyle = "Quiet", IsActive = true };
        
        _roommatesContext.Roommates.AddRange(r1, r2);
        await _roommatesContext.SaveChangesAsync();

        // Act
        var score = await service.CalculateMatchScoreAsync(1, 2);

        // Assert
        score.Should().BeInRange(0, 100);
    }

    [Fact]
    public async Task GetMatchesForUserAsync_ExistentUser_ShouldReturnMatches()
    {
        // Arrange
        var service = new RoommateMatchingService(_roommatesContext, _mockRoommateService.Object);
        
        var user = new Roommate { UserId = 1, RoommateId = 1, IsActive = true, PreferredLocation = "City A" };
        var candidate = new Roommate { UserId = 2, RoommateId = 2, IsActive = true, PreferredLocation = "City A" };
        
        _roommatesContext.Roommates.AddRange(user, candidate);
        await _roommatesContext.SaveChangesAsync();

        // Act
        var matches = await service.GetMatchesForUserAsync(1);

        // Assert
        matches.Should().NotBeEmpty();
        matches.First().RoommateId.Should().Be(candidate.RoommateId);
    }
}
