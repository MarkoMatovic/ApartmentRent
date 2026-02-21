using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Lander;
using Lander.src.Modules.Roommates.Implementation;
using Lander.src.Modules.Roommates.Models;
using Lander.src.Modules.Roommates.Dtos.InputDto;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using System.Security.Claims;

namespace LandlordApp.Tests.Services;

public class RoommateServiceTests : IDisposable
{
    private readonly RoommatesContext _roommatesContext;
    private readonly UsersContext _usersContext;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly RoommateService _roommateService;
    
    private readonly int _testUserId = 1;
    private readonly Guid _testUserGuid = Guid.NewGuid();

    public RoommateServiceTests()
    {
        var roommatesOptions = new DbContextOptionsBuilder<RoommatesContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var usersOptions = new DbContextOptionsBuilder<UsersContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _roommatesContext = new RoommatesContext(roommatesOptions);
        _usersContext = new UsersContext(usersOptions);
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockCache = new Mock<IMemoryCache>();

        // Setup memory cache mock to just pass through or return false for TryGetValue
        object? outValue = null;
        _mockCache.Setup(m => m.TryGetValue(It.IsAny<object>(), out outValue)).Returns(false);
        _mockCache.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(new Mock<ICacheEntry>().Object);

        SetupUserContext(_testUserId, _testUserGuid);

        _roommateService = new RoommateService(
            _roommatesContext,
            _usersContext,
            _mockHttpContextAccessor.Object,
            _mockCache.Object
        );

        SeedTestData().Wait();
    }

    private async Task SeedTestData()
    {
        var user = new User
        {
            UserId = _testUserId,
            UserGuid = _testUserGuid,
            FirstName = "Test",
            LastName = "User",
            Email = "test@roommate.com",
            Password = "TestPassword123",
            IsActive = true
        };
        _usersContext.Users.Add(user);
        await _usersContext.SaveChangesAsync();
    }

    private void SetupUserContext(int userId, Guid userGuid)
    {
        var claims = new List<Claim>
        {
            new Claim("userId", userId.ToString()),
            new Claim("sub", userGuid.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
    }

    public void Dispose()
    {
        _roommatesContext.Database.EnsureDeleted();
        _usersContext.Database.EnsureDeleted();
        _roommatesContext.Dispose();
        _usersContext.Dispose();
    }

    [Fact]
    public async Task CreateRoommateAsync_NewProfile_ShouldCreateRoommate()
    {
        // Arrange
        var input = new RoommateInputDto
        {
            Bio = "I am a clean person",
            PreferredLocation = "Beograd",
            BudgetMax = 500
        };

        // Act
        var result = await _roommateService.CreateRoommateAsync(_testUserId, input);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(_testUserId);
        result.Bio.Should().Be("I am a clean person");

        var dbRoommate = await _roommatesContext.Roommates.FirstOrDefaultAsync(r => r.UserId == _testUserId);
        dbRoommate.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllRoommatesAsync_FilterByLocation_ShouldReturnMatching()
    {
        // Arrange
        _roommatesContext.Roommates.Add(new Roommate 
        { 
            UserId = _testUserId, 
            PreferredLocation = "Novi Sad", 
            IsActive = true 
        });
        _roommatesContext.Roommates.Add(new Roommate 
        { 
            UserId = 2, 
            PreferredLocation = "Beograd", 
            IsActive = true 
        });
        await _roommatesContext.SaveChangesAsync();

        // Act
        var result = await _roommateService.GetAllRoommatesAsync(location: "Novi Sad");

        // Assert
        result.Should().HaveCount(1);
        result.First().PreferredLocation.Should().Be("Novi Sad");
    }

    [Fact]
    public async Task GetAllRoommatesAsync_NoResultsFound_ShouldReturnEmptyList()
    {
        // Act
        var result = await _roommateService.GetAllRoommatesAsync(location: "NonExistentLocation");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllRoommatesAsync_WithMultipleFilters_ShouldReturnCorrectResults()
    {
        // Arrange
        _roommatesContext.Roommates.AddRange(
            new Roommate { UserId = 1, PreferredLocation = "Beograd", BudgetMax = 500, IsActive = true, SmokingAllowed = false },
            new Roommate { UserId = 2, PreferredLocation = "Beograd", BudgetMax = 300, IsActive = true, SmokingAllowed = true },
            new Roommate { UserId = 3, PreferredLocation = "Novi Sad", BudgetMax = 500, IsActive = true, SmokingAllowed = false }
        );
        await _roommatesContext.SaveChangesAsync();

        // Act
        var result = await _roommateService.GetAllRoommatesAsync(
            location: "Beograd", 
            maxBudget: 400, 
            smokingAllowed: true
        );

        // Assert
        result.Should().HaveCount(1);
        result.First().UserId.Should().Be(2);
    }

    [Fact]
    public async Task GetRoommateByIdAsync_ExistentId_ShouldReturnRoommate()
    {
        // Arrange
        var roommate = new Roommate { UserId = _testUserId, IsActive = true };
        _roommatesContext.Roommates.Add(roommate);
        await _roommatesContext.SaveChangesAsync();

        // Act
        var result = await _roommateService.GetRoommateByIdAsync(roommate.RoommateId);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(_testUserId);
    }

    [Fact]
    public async Task DeleteRoommateAsync_Ownership_ShouldDeactivate()
    {
        // Arrange
        var roommate = new Roommate { UserId = _testUserId, IsActive = true };
        _roommatesContext.Roommates.Add(roommate);
        await _roommatesContext.SaveChangesAsync();

        // Act
        var result = await _roommateService.DeleteRoommateAsync(roommate.RoommateId, _testUserId);

        // Assert
        result.Should().BeTrue();
        var deactivated = await _roommatesContext.Roommates.FindAsync(roommate.RoommateId);
        deactivated!.IsActive.Should().BeFalse();
    }
}
