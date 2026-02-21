using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Lander;
using Lander.src.Modules.ApartmentApplications.Implementation;
using Lander.src.Modules.ApartmentApplications.Models;
using Lander.src.Modules.Listings.Dtos.Dto;
using Lander.src.Modules.Listings.Interfaces;
using Lander.src.Modules.Listings.Models;
using Lander.src.Notifications.NotificationsHub;
using Lander.src.Modules.Users.Interfaces.UserInterface;

namespace LandlordApp.Tests.Services;

public class ApartmentApplicationServiceTests : IDisposable
{
    private readonly ApplicationsContext _context;
    private readonly Mock<IApartmentService> _mockApartmentService;
    private readonly Mock<IHubContext<NotificationHub>> _mockNotificationHub;
    private readonly Mock<IUserInterface> _mockUserService;
    private readonly ApartmentApplicationService _service;

    private const int TenantUserId    = 10;
    private const int LandlordUserId  = 20;
    private const int ApartmentId     = 1;

    public ApartmentApplicationServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationsContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationsContext(options);

        _mockApartmentService  = new Mock<IApartmentService>();
        _mockNotificationHub   = new Mock<IHubContext<NotificationHub>>();
        _mockUserService       = new Mock<IUserInterface>();

        // Setup notification hub (so calls don't throw)
        var mockClients      = new Mock<IHubClients>();
        var mockClientProxy  = new Mock<IClientProxy>();
        mockClients.Setup(x => x.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
        _mockNotificationHub.Setup(x => x.Clients).Returns(mockClients.Object);

        // Default apartment stub
        _mockApartmentService
            .Setup(x => x.GetApartmentByIdAsync(ApartmentId))
            .ReturnsAsync(new GetApartmentDto { ApartmentId = ApartmentId, Title = "Test Apartment", LandlordId = LandlordUserId });

        // Default apartment list for landlord
        _mockApartmentService
            .Setup(x => x.GetApartmentsByLandlordIdAsync(LandlordUserId))
            .ReturnsAsync(new List<ApartmentDto> { new() { ApartmentId = ApartmentId, Title = "Test Apartment" } });

        _service = new ApartmentApplicationService(
            _context,
            _mockApartmentService.Object,
            _mockNotificationHub.Object,
            _mockUserService.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region ApplyForApartmentAsync Tests

    [Fact]
    public async Task ApplyForApartmentAsync_NewApplication_ShouldCreateAndReturnApplication()
    {
        // Act
        var result = await _service.ApplyForApartmentAsync(TenantUserId, ApartmentId);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(TenantUserId);
        result.ApartmentId.Should().Be(ApartmentId);
        result.Status.Should().Be("Pending");

        var inDb = await _context.ApartmentApplications.FirstOrDefaultAsync();
        inDb.Should().NotBeNull();
        inDb!.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task ApplyForApartmentAsync_DuplicateApplication_ShouldReturnNull()
    {
        // Arrange — first apply
        await _service.ApplyForApartmentAsync(TenantUserId, ApartmentId);

        // Act — second apply for same apartment
        var result = await _service.ApplyForApartmentAsync(TenantUserId, ApartmentId);

        // Assert
        result.Should().BeNull("duplicate applications are not allowed");

        var count = await _context.ApartmentApplications.CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task ApplyForApartmentAsync_DifferentTenants_ShouldBothApply()
    {
        // Arrange
        var tenant2 = TenantUserId + 1;

        // Act
        var r1 = await _service.ApplyForApartmentAsync(TenantUserId, ApartmentId);
        var r2 = await _service.ApplyForApartmentAsync(tenant2, ApartmentId);

        // Assert
        r1.Should().NotBeNull();
        r2.Should().NotBeNull();

        var count = await _context.ApartmentApplications.CountAsync();
        count.Should().Be(2);
    }

    [Fact]
    public async Task ApplyForApartmentAsync_ShouldNotifyLandlord()
    {
        // Act
        await _service.ApplyForApartmentAsync(TenantUserId, ApartmentId);

        // Assert — SignalR ReceiveNotification called for landlord group
        var mockClients = _mockNotificationHub.Object.Clients;
        Mock.Get(mockClients).Verify(
            x => x.Group(LandlordUserId.ToString()),
            Times.AtLeastOnce);
    }

    #endregion

    #region GetTenantApplicationsAsync Tests

    [Fact]
    public async Task GetTenantApplicationsAsync_NoApplications_ShouldReturnEmpty()
    {
        // Act
        var result = await _service.GetTenantApplicationsAsync(TenantUserId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTenantApplicationsAsync_WithApplications_ShouldReturnOnlyTenantOnes()
    {
        // Arrange — two applications for tenant, one for another user
        _context.ApartmentApplications.AddRange(
            new ApartmentApplication { UserId = TenantUserId, ApartmentId = ApartmentId, Status = "Pending", ApplicationDate = DateTime.UtcNow, CreatedDate = DateTime.UtcNow, CreatedByGuid = Guid.NewGuid() },
            new ApartmentApplication { UserId = TenantUserId, ApartmentId = 2,           Status = "Approved", ApplicationDate = DateTime.UtcNow, CreatedDate = DateTime.UtcNow, CreatedByGuid = Guid.NewGuid() },
            new ApartmentApplication { UserId = 999,           ApartmentId = ApartmentId, Status = "Pending", ApplicationDate = DateTime.UtcNow, CreatedDate = DateTime.UtcNow, CreatedByGuid = Guid.NewGuid() }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetTenantApplicationsAsync(TenantUserId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(a => a.UserId == TenantUserId);
    }

    #endregion

    #region GetLandlordApplicationsAsync Tests

    [Fact]
    public async Task GetLandlordApplicationsAsync_ShouldReturnOnlyApplicationsForLandlordApartments()
    {
        // Arrange
        _context.ApartmentApplications.AddRange(
            new ApartmentApplication { UserId = TenantUserId, ApartmentId = ApartmentId, Status = "Pending", ApplicationDate = DateTime.UtcNow, CreatedDate = DateTime.UtcNow, CreatedByGuid = Guid.NewGuid() },
            new ApartmentApplication { UserId = TenantUserId, ApartmentId = 999,         Status = "Pending", ApplicationDate = DateTime.UtcNow, CreatedDate = DateTime.UtcNow, CreatedByGuid = Guid.NewGuid() } // not landlord's apt
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetLandlordApplicationsAsync(LandlordUserId);

        // Assert
        result.Should().HaveCount(1);
        result.First().ApartmentId.Should().Be(ApartmentId);
    }

    [Fact]
    public async Task GetLandlordApplicationsAsync_NoApartments_ShouldReturnEmpty()
    {
        // Arrange — landlord with no apartments
        _mockApartmentService
            .Setup(x => x.GetApartmentsByLandlordIdAsync(999))
            .ReturnsAsync(new List<ApartmentDto>());

        // Act
        var result = await _service.GetLandlordApplicationsAsync(999);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region UpdateApplicationStatusAsync Tests

    [Fact]
    public async Task ApplyForApartmentAsync_ApartmentNotFound_ShouldThrowArgumentException()
    {
        // Arrange
        _mockApartmentService.Setup(x => x.GetApartmentByIdAsync(999)).ReturnsAsync((GetApartmentDto)null!);

        // Act
        var act = async () => await _service.ApplyForApartmentAsync(TenantUserId, 999);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Apartment not found");
    }

    [Fact]
    public async Task UpdateApplicationStatusAsync_SameStatus_ShouldSucceed()
    {
        // Arrange
        var app = new ApartmentApplication { UserId = TenantUserId, ApartmentId = ApartmentId, Status = "Approved", ApplicationDate = DateTime.UtcNow, CreatedDate = DateTime.UtcNow, CreatedByGuid = Guid.NewGuid() };
        _context.ApartmentApplications.Add(app);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.UpdateApplicationStatusAsync(app.ApplicationId, "Approved", LandlordUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be("Approved");
    }

    [Fact]
    public async Task UpdateApplicationStatusAsync_LandlordApproves_ShouldUpdateStatus()
    {
        // Arrange
        var app = new ApartmentApplication { UserId = TenantUserId, ApartmentId = ApartmentId, Status = "Pending", ApplicationDate = DateTime.UtcNow, CreatedDate = DateTime.UtcNow, CreatedByGuid = Guid.NewGuid() };
        _context.ApartmentApplications.Add(app);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.UpdateApplicationStatusAsync(app.ApplicationId, "Approved", LandlordUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be("Approved");
    }

    [Fact]
    public async Task UpdateApplicationStatusAsync_LandlordRejects_ShouldUpdateStatus()
    {
        // Arrange
        var app = new ApartmentApplication { UserId = TenantUserId, ApartmentId = ApartmentId, Status = "Pending", ApplicationDate = DateTime.UtcNow, CreatedDate = DateTime.UtcNow, CreatedByGuid = Guid.NewGuid() };
        _context.ApartmentApplications.Add(app);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.UpdateApplicationStatusAsync(app.ApplicationId, "Rejected", LandlordUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be("Rejected");
    }

    [Fact]
    public async Task UpdateApplicationStatusAsync_WrongLandlord_ShouldThrowUnauthorized()
    {
        // Arrange
        var app = new ApartmentApplication { UserId = TenantUserId, ApartmentId = ApartmentId, Status = "Pending", ApplicationDate = DateTime.UtcNow, CreatedDate = DateTime.UtcNow, CreatedByGuid = Guid.NewGuid() };
        _context.ApartmentApplications.Add(app);
        await _context.SaveChangesAsync();

        // Act — wrong landlord (not the owner of ApartmentId)
        var act = async () => await _service.UpdateApplicationStatusAsync(app.ApplicationId, "Approved", 9999);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*not the landlord*");
    }

    [Fact]
    public async Task UpdateApplicationStatusAsync_NonExistentApplication_ShouldReturnNull()
    {
        // Act
        var result = await _service.UpdateApplicationStatusAsync(99999, "Approved", LandlordUserId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetApplicationByIdAsync Tests

    [Fact]
    public async Task GetApplicationByIdAsync_Existing_ShouldReturn()
    {
        // Arrange
        var app = new ApartmentApplication { UserId = TenantUserId, ApartmentId = ApartmentId, Status = "Pending", ApplicationDate = DateTime.UtcNow, CreatedDate = DateTime.UtcNow, CreatedByGuid = Guid.NewGuid() };
        _context.ApartmentApplications.Add(app);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetApplicationByIdAsync(app.ApplicationId);

        // Assert
        result.Should().NotBeNull();
        result!.ApplicationId.Should().Be(app.ApplicationId);
    }

    [Fact]
    public async Task GetApplicationByIdAsync_NonExistent_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetApplicationByIdAsync(99999);

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
