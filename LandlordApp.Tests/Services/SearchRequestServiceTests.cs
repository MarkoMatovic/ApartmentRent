using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Lander;
using Lander.src.Modules.SearchRequests.Implementation;
using Lander.src.Modules.SearchRequests.Dtos.InputDto;
using Lander.src.Modules.SearchRequests.Models;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;

namespace LandlordApp.Tests.Services;

public class SearchRequestServiceTests : IDisposable
{
    private readonly SearchRequestsContext _searchContext;
    private readonly UsersContext _usersContext;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly SearchRequestService _service;

    private const int UserId1 = 1;
    private const int UserId2 = 2;
    private readonly Guid _userGuid = Guid.NewGuid();

    public SearchRequestServiceTests()
    {
        var searchOpts = new DbContextOptionsBuilder<SearchRequestsContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var usersOpts = new DbContextOptionsBuilder<UsersContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _searchContext = new SearchRequestsContext(searchOpts);
        _usersContext  = new UsersContext(usersOpts);

        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        SetupHttpContext(_userGuid);

        // Seed test users
        _usersContext.Users.AddRange(
            new User { UserId = UserId1, UserGuid = Guid.NewGuid(), FirstName = "Marko",  LastName = "M", Email = "marko@test.com",  Password = "hash", IsActive = true, CreatedDate = DateTime.UtcNow },
            new User { UserId = UserId2, UserGuid = Guid.NewGuid(), FirstName = "Ana",    LastName = "A", Email = "ana@test.com",    Password = "hash", IsActive = true, CreatedDate = DateTime.UtcNow }
        );
        _usersContext.SaveChanges();

        _service = new SearchRequestService(_searchContext, _usersContext, _mockHttpContextAccessor.Object);
    }

    private void SetupHttpContext(Guid userGuid)
    {
        var claims    = new List<Claim> { new("sub", userGuid.ToString()) };
        var identity  = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var ctx       = new DefaultHttpContext { User = principal };
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(ctx);
    }

    public void Dispose()
    {
        _searchContext.Database.EnsureDeleted();
        _usersContext.Database.EnsureDeleted();
        _searchContext.Dispose();
        _usersContext.Dispose();
    }

    // ───────────────────────────────────────────────────── helpers

    private SearchRequestInputDto MakeInput(string city = "Beograd", SearchRequestType type = SearchRequestType.LookingForApartment)
        => new()
        {
            Title       = $"Looking for apartment in {city}",
            Description = "Nice place needed",
            City        = city,
            RequestType = type,
            BudgetMin   = 200,
            BudgetMax   = 500
        };

    private async Task<SearchRequest> SeedRequest(int userId, string city, decimal budgetMin = 200, decimal budgetMax = 500, bool isActive = true)
    {
        var req = new SearchRequest
        {
            UserId      = userId,
            Title       = $"Looking in {city}",
            City        = city,
            RequestType = SearchRequestType.LookingForApartment,
            BudgetMin   = budgetMin,
            BudgetMax   = budgetMax,
            IsActive    = isActive,
            CreatedDate = DateTime.UtcNow,
            CreatedByGuid = Guid.NewGuid()
        };
        _searchContext.SearchRequests.Add(req);
        await _searchContext.SaveChangesAsync();
        return req;
    }

    #region GetAllSearchRequestsAsync Tests

    [Fact]
    public async Task GetAllSearchRequestsAsync_NoFilters_ShouldReturnAllActive()
    {
        // Arrange
        await SeedRequest(UserId1, "Beograd");
        await SeedRequest(UserId2, "Novi Sad");
        await SeedRequest(UserId1, "Nis", isActive: false);   // should be excluded

        // Act
        var result = await _service.GetAllSearchRequestsAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllSearchRequestsAsync_FilterByCity_ShouldReturnMatchingOnly()
    {
        // Arrange
        await SeedRequest(UserId1, "Beograd");
        await SeedRequest(UserId2, "Novi Sad");

        // Act
        var result = await _service.GetAllSearchRequestsAsync(city: "Beograd");

        // Assert
        result.Should().HaveCount(1);
        result.First().City.Should().Be("Beograd");
    }

    [Fact]
    public async Task GetAllSearchRequestsAsync_FilterByBudget_ShouldReturnMatchingOnly()
    {
        // Arrange
        await SeedRequest(UserId1, "Beograd", budgetMin: 200, budgetMax: 400);
        await SeedRequest(UserId2, "Novi Sad", budgetMin: 1000, budgetMax: 2000);

        // Act — looking for requests where max budget >= 300 (tenant offers at least 300)
        var result = await _service.GetAllSearchRequestsAsync(minBudget: 500);

        // Assert
        result.Should().HaveCount(1);
        result.First().City.Should().Be("Novi Sad");
    }

    #endregion

    #region GetAllSearchRequestsAsync (Paged) Tests

    [Fact]
    public async Task GetAllSearchRequestsAsync_Paged_ShouldReturnCorrectPage()
    {
        // Arrange — 5 requests
        for (int i = 0; i < 5; i++)
            await SeedRequest(UserId1, $"City{i}");

        // Act — page 1, size 2
        var result = await _service.GetAllSearchRequestsAsync(null, null, null, null, page: 1, pageSize: 2);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetAllSearchRequestsAsync_Paged_LastPage_ShouldReturnRemainder()
    {
        // Arrange — 5 requests
        for (int i = 0; i < 5; i++)
            await SeedRequest(UserId1, $"City{i}");

        // Act — page 3, size 2 → 1 remaining
        var result = await _service.GetAllSearchRequestsAsync(null, null, null, null, page: 3, pageSize: 2);

        // Assert
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(5);
    }

    #endregion

    #region GetSearchRequestByIdAsync Tests

    [Fact]
    public async Task GetSearchRequestByIdAsync_ExistingActive_ShouldReturn()
    {
        // Arrange
        var req = await SeedRequest(UserId1, "Beograd");

        // Act
        var result = await _service.GetSearchRequestByIdAsync(req.SearchRequestId);

        // Assert
        result.Should().NotBeNull();
        result!.City.Should().Be("Beograd");
        result.FirstName.Should().Be("Marko");
    }

    [Fact]
    public async Task GetSearchRequestByIdAsync_NonExistent_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetSearchRequestByIdAsync(99999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSearchRequestByIdAsync_Inactive_ShouldReturnNull()
    {
        // Arrange
        var req = await SeedRequest(UserId1, "Beograd", isActive: false);

        // Act
        var result = await _service.GetSearchRequestByIdAsync(req.SearchRequestId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetSearchRequestsByUserIdAsync Tests

    [Fact]
    public async Task GetSearchRequestsByUserIdAsync_ShouldReturnOnlyForUser()
    {
        // Arrange
        await SeedRequest(UserId1, "Beograd");
        await SeedRequest(UserId1, "Novi Sad");
        await SeedRequest(UserId2, "Nis");

        // Act
        var result = await _service.GetSearchRequestsByUserIdAsync(UserId1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.UserId == UserId1);
    }

    #endregion

    #region CreateSearchRequestAsync Tests

    [Fact]
    public async Task CreateSearchRequestAsync_ValidInput_ShouldCreate()
    {
        // Act
        var result = await _service.CreateSearchRequestAsync(UserId1, MakeInput());

        // Assert
        result.Should().NotBeNull();
        result.City.Should().Be("Beograd");
        result.IsActive.Should().BeTrue();
        result.UserId.Should().Be(UserId1);
    }

    [Fact]
    public async Task CreateSearchRequestAsync_SaleType_ShouldRespectType()
    {
        // Act
        var result = await _service.CreateSearchRequestAsync(UserId1, MakeInput(type: SearchRequestType.LookingForHouse));

        // Assert
        result.RequestType.Should().Be(SearchRequestType.LookingForHouse);
    }

    #endregion

    #region UpdateSearchRequestAsync Tests

    [Fact]
    public async Task UpdateSearchRequestAsync_ValidOwner_ShouldUpdate()
    {
        // Arrange
        var req = await SeedRequest(UserId1, "Beograd");
        var input = MakeInput("Novi Sad");
        input.Title = "Updated title";

        // Act
        var result = await _service.UpdateSearchRequestAsync(req.SearchRequestId, UserId1, input);

        // Assert
        result.City.Should().Be("Novi Sad");
        result.Title.Should().Be("Updated title");
    }

    [Fact]
    public async Task UpdateSearchRequestAsync_WrongUser_ShouldThrowException()
    {
        // Arrange
        var req = await SeedRequest(UserId1, "Beograd");

        // Act — UserId2 tries to update UserId1's request
        var act = async () => await _service.UpdateSearchRequestAsync(req.SearchRequestId, UserId2, MakeInput());

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("*not found*");
    }

    [Fact]
    public async Task UpdateSearchRequestAsync_NonExistent_ShouldThrowException()
    {
        // Act
        var act = async () => await _service.UpdateSearchRequestAsync(99999, UserId1, MakeInput());

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region DeleteSearchRequestAsync Tests

    [Fact]
    public async Task DeleteSearchRequestAsync_ValidOwner_ShouldSoftDelete()
    {
        // Arrange
        var req = await SeedRequest(UserId1, "Beograd");

        // Act
        var result = await _service.DeleteSearchRequestAsync(req.SearchRequestId, UserId1);

        // Assert
        result.Should().BeTrue();

        var inDb = await _searchContext.SearchRequests.FirstOrDefaultAsync(x => x.SearchRequestId == req.SearchRequestId);
        inDb!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteSearchRequestAsync_WrongUser_ShouldReturnFalse()
    {
        // Arrange
        var req = await SeedRequest(UserId1, "Beograd");

        // Act — UserId2 tries to delete UserId1's request
        var result = await _service.DeleteSearchRequestAsync(req.SearchRequestId, UserId2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteSearchRequestAsync_NonExistent_ShouldReturnFalse()
    {
        // Act
        var result = await _service.DeleteSearchRequestAsync(99999, UserId1);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
