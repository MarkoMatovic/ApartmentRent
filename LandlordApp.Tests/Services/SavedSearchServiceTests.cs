using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Lander;
using Lander.src.Modules.SavedSearches.Implementation;
using Lander.src.Modules.SavedSearches.Dtos.InputDto;
using Lander.src.Modules.SavedSearches.Models;

namespace LandlordApp.Tests.Services;

public class SavedSearchServiceTests : IDisposable
{
    private readonly SavedSearchesContext _context;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly SavedSearchService _service;

    private const int UserId1 = 1;
    private const int UserId2 = 2;
    private readonly Guid _userGuid = Guid.NewGuid();

    public SavedSearchServiceTests()
    {
        var options = new DbContextOptionsBuilder<SavedSearchesContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new SavedSearchesContext(options);
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        SetupHttpContext(_userGuid);

        _service = new SavedSearchService(_context, _mockHttpContextAccessor.Object);
    }

    private void SetupHttpContext(Guid userGuid)
    {
        var claims = new List<Claim> { new("sub", userGuid.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetSavedSearchesByUserIdAsync Tests

    [Fact]
    public async Task GetSavedSearchesByUserIdAsync_NoSearches_ShouldReturnEmpty()
    {
        // Act
        var result = await _service.GetSavedSearchesByUserIdAsync(UserId1);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSavedSearchesByUserIdAsync_WithSearches_ShouldReturnOnlyUserSearches()
    {
        // Arrange
        _context.SavedSearches.AddRange(
            new SavedSearch { UserId = UserId1, Name = "Search 1", SearchType = "rent", IsActive = true, CreatedDate = DateTime.UtcNow },
            new SavedSearch { UserId = UserId1, Name = "Search 2", SearchType = "sale", IsActive = true, CreatedDate = DateTime.UtcNow },
            new SavedSearch { UserId = UserId2, Name = "Other",    SearchType = "rent", IsActive = true, CreatedDate = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSavedSearchesByUserIdAsync(UserId1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(s => s.UserId == UserId1);
    }

    [Fact]
    public async Task GetSavedSearchesByUserIdAsync_InactiveSearches_ShouldBeExcluded()
    {
        // Arrange
        _context.SavedSearches.AddRange(
            new SavedSearch { UserId = UserId1, Name = "Active",   SearchType = "rent", IsActive = true,  CreatedDate = DateTime.UtcNow },
            new SavedSearch { UserId = UserId1, Name = "Inactive", SearchType = "rent", IsActive = false, CreatedDate = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSavedSearchesByUserIdAsync(UserId1);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Active");
    }

    #endregion

    #region GetSavedSearchByIdAsync Tests

    [Fact]
    public async Task GetSavedSearchByIdAsync_Existing_ShouldReturn()
    {
        // Arrange
        var ss = new SavedSearch { UserId = UserId1, Name = "My Search", SearchType = "rent", IsActive = true, CreatedDate = DateTime.UtcNow };
        _context.SavedSearches.Add(ss);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSavedSearchByIdAsync(ss.SavedSearchId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("My Search");
        result.UserId.Should().Be(UserId1);
    }

    [Fact]
    public async Task GetSavedSearchByIdAsync_NonExistent_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetSavedSearchByIdAsync(99999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSavedSearchByIdAsync_Inactive_ShouldReturnNull()
    {
        // Arrange
        var ss = new SavedSearch { UserId = UserId1, Name = "Soft Deleted", SearchType = "rent", IsActive = false, CreatedDate = DateTime.UtcNow };
        _context.SavedSearches.Add(ss);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSavedSearchByIdAsync(ss.SavedSearchId);

        // Assert
        result.Should().BeNull("soft-deleted searches should not be returned");
    }

    #endregion

    #region CreateSavedSearchAsync Tests

    [Fact]
    public async Task CreateSavedSearchAsync_ValidInput_ShouldCreateAndReturn()
    {
        // Arrange
        var input = new SavedSearchInputDto
        {
            Name = "Beograd stanovi",
            SearchType = "rent",
            FiltersJson = "{\"city\":\"Beograd\"}",
            EmailNotificationsEnabled = true
        };

        // Act
        var result = await _service.CreateSavedSearchAsync(UserId1, input);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Beograd stanovi");
        result.SearchType.Should().Be("rent");
        result.EmailNotificationsEnabled.Should().BeTrue();
        result.IsActive.Should().BeTrue();
        result.UserId.Should().Be(UserId1);

        var inDb = await _context.SavedSearches.FirstOrDefaultAsync();
        inDb.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateSavedSearchAsync_NotificationsDisabled_ShouldRespectSetting()
    {
        // Arrange
        var input = new SavedSearchInputDto
        {
            Name = "Quiet Search",
            SearchType = "sale",
            EmailNotificationsEnabled = false
        };

        // Act
        var result = await _service.CreateSavedSearchAsync(UserId1, input);

        // Assert
        result.EmailNotificationsEnabled.Should().BeFalse();
    }

    #endregion

    #region UpdateSavedSearchAsync Tests

    [Fact]
    public async Task UpdateSavedSearchAsync_ValidOwner_ShouldUpdate()
    {
        // Arrange
        var ss = new SavedSearch { UserId = UserId1, Name = "Old Name", SearchType = "rent", IsActive = true, CreatedDate = DateTime.UtcNow };
        _context.SavedSearches.Add(ss);
        await _context.SaveChangesAsync();

        var input = new SavedSearchInputDto { Name = "New Name", SearchType = "sale", EmailNotificationsEnabled = false };

        // Act
        var result = await _service.UpdateSavedSearchAsync(ss.SavedSearchId, UserId1, input);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Name");
        result.SearchType.Should().Be("sale");
    }

    [Fact]
    public async Task UpdateSavedSearchAsync_WrongUser_ShouldThrowException()
    {
        // Arrange
        var ss = new SavedSearch { UserId = UserId1, Name = "User1 Search", SearchType = "rent", IsActive = true, CreatedDate = DateTime.UtcNow };
        _context.SavedSearches.Add(ss);
        await _context.SaveChangesAsync();

        var input = new SavedSearchInputDto { Name = "Hack", SearchType = "rent" };

        // Act — UserId2 tries to update UserId1's search
        var act = async () => await _service.UpdateSavedSearchAsync(ss.SavedSearchId, UserId2, input);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("*not found*");
    }

    [Fact]
    public async Task UpdateSavedSearchAsync_NonExistent_ShouldThrowException()
    {
        // Arrange
        var input = new SavedSearchInputDto { Name = "Test", SearchType = "rent" };

        // Act
        var act = async () => await _service.UpdateSavedSearchAsync(99999, UserId1, input);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region DeleteSavedSearchAsync Tests

    [Fact]
    public async Task DeleteSavedSearchAsync_ValidOwner_ShouldSoftDelete()
    {
        // Arrange
        var ss = new SavedSearch { UserId = UserId1, Name = "To Delete", SearchType = "rent", IsActive = true, CreatedDate = DateTime.UtcNow };
        _context.SavedSearches.Add(ss);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteSavedSearchAsync(ss.SavedSearchId, UserId1);

        // Assert
        result.Should().BeTrue();

        var inDb = await _context.SavedSearches.FirstOrDefaultAsync(x => x.SavedSearchId == ss.SavedSearchId);
        inDb!.IsActive.Should().BeFalse("soft delete sets IsActive = false");
    }

    [Fact]
    public async Task DeleteSavedSearchAsync_WrongUser_ShouldReturnFalse()
    {
        // Arrange
        var ss = new SavedSearch { UserId = UserId1, Name = "Protected", SearchType = "rent", IsActive = true, CreatedDate = DateTime.UtcNow };
        _context.SavedSearches.Add(ss);
        await _context.SaveChangesAsync();

        // Act — UserId2 tries to delete UserId1's search
        var result = await _service.DeleteSavedSearchAsync(ss.SavedSearchId, UserId2);

        // Assert
        result.Should().BeFalse();

        var inDb = await _context.SavedSearches.FirstOrDefaultAsync(x => x.SavedSearchId == ss.SavedSearchId);
        inDb!.IsActive.Should().BeTrue("the owner's item should remain active");
    }

    [Fact]
    public async Task DeleteSavedSearchAsync_NonExistent_ShouldReturnFalse()
    {
        // Act
        var result = await _service.DeleteSavedSearchAsync(99999, UserId1);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
