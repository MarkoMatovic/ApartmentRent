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
            UserId    = _testUserId,
            UserGuid  = _testUserGuid,
            FirstName = "Test",
            LastName  = "User",
            Email     = "test@test.com",
            Password  = "hashed",
            IsActive  = true
        };
        _usersContext.Users.Add(user);
        await _usersContext.SaveChangesAsync();
    }

    private void SetupUserContext(int userId, Guid userGuid)
    {
        var claims = new List<Claim> { new Claim("userId", userId.ToString()), new Claim("sub", userGuid.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
    }

    public void Dispose()
    {
        _roommatesContext.Database.EnsureDeleted();
        _usersContext.Database.EnsureDeleted();
        _roommatesContext.Dispose();
        _usersContext.Dispose();
    }

    #region Create & Update

    [Fact]
    public async Task CreateRoommateAsync_NewProfile_ShouldSucceed()
    {
        var input = new RoommateInputDto { Bio = "Bio", PreferredLocation = "Beograd", BudgetMax = 500 };
        var result = await _roommateService.CreateRoommateAsync(_testUserId, input);
        result.UserId.Should().Be(_testUserId);
        (await _roommatesContext.Roommates.AnyAsync(r => r.UserId == _testUserId)).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateRoommateAsync_Update_ShouldPersistChange()
    {
        var roommate = new Roommate { UserId = _testUserId, Bio = "Old Bio", Hobbies = "Quiet", IsActive = true };
        _roommatesContext.Roommates.Add(roommate);
        await _roommatesContext.SaveChangesAsync();

        // Service does full replace, so pass both fields
        var update = new RoommateInputDto { Bio = "New Bio", Hobbies = "Quiet" };
        await _roommateService.UpdateRoommateAsync(roommate.RoommateId, _testUserId, update);

        var dbRoommate = await _roommatesContext.Roommates.FindAsync(roommate.RoommateId);
        dbRoommate!.Bio.Should().Be("New Bio");
        dbRoommate.Hobbies.Should().Be("Quiet"); // Passed explicitly
    }

    #endregion

    #region Retrieval & Filtering

    [Fact]
    public async Task GetRoommateByUserIdAsync_Existent_ShouldReturn()
    {
        _roommatesContext.Roommates.Add(new Roommate { UserId = _testUserId, IsActive = true });
        await _roommatesContext.SaveChangesAsync();

        var result = await _roommateService.GetRoommateByUserIdAsync(_testUserId);
        result.Should().NotBeNull();
        result!.UserId.Should().Be(_testUserId);
    }

    [Fact]
    public async Task GetAllRoommatesAsync_ComplexFilters_ShouldWork()
    {
        _roommatesContext.Roommates.AddRange(
            new Roommate { UserId = 1, PreferredLocation = "A", BudgetMax = 100, SmokingAllowed = false, IsActive = true },
            new Roommate { UserId = 2, PreferredLocation = "A", BudgetMax = 200, SmokingAllowed = true, IsActive = true },
            new Roommate { UserId = 3, PreferredLocation = "B", BudgetMax = 100, SmokingAllowed = false, IsActive = true }
        );
        await _roommatesContext.SaveChangesAsync();

        var results = await _roommateService.GetAllRoommatesAsync(location: "A", maxBudget: 150, smokingAllowed: false);
        results.Should().HaveCount(1);
        results.First().UserId.Should().Be(1);
    }

    #endregion

    #region Deletion & Security

    [Fact]
    public async Task DeleteRoommateAsync_NotOwner_ShouldReturnFalse()
    {
        var roommate = new Roommate { UserId = 999, IsActive = true };
        _roommatesContext.Roommates.Add(roommate);
        await _roommatesContext.SaveChangesAsync();

        var result = await _roommateService.DeleteRoommateAsync(roommate.RoommateId, _testUserId);
        result.Should().BeFalse();
        (await _roommatesContext.Roommates.FindAsync(roommate.RoommateId))!.IsActive.Should().BeTrue();
    }

    #endregion
}
