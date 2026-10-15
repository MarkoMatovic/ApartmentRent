using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Lander.src.Notifications.Dtos.InputDto;
using Lander.src.Notifications.Interfaces;
using Lander;
using Lander.src.Modules.ApartmentApplications.Implementation;
using Lander.src.Modules.ApartmentApplications.Models;
using Lander.src.Modules.Listings.Dtos.Dto;
using Lander.src.Modules.Listings.Interfaces;
using Lander.src.Modules.Listings.Models;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Modules.Communication.Interfaces;
using Lander.src.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace LandlordApp.Tests.Services;

public class ApartmentApplicationServiceTests : IDisposable
{
    private readonly ApplicationsContext _context;
    private readonly Mock<IApartmentService> _mockApartmentService;
    private readonly Mock<IUserInterface> _mockUserService;
    private readonly Mock<IEmailService> _mockEmail = new();
    private readonly Mock<IPresenceTracker> _mockPresence = new();
    private readonly Mock<INotificationService> _mockBell = new();
    private readonly List<CreateNotificationInputDto> _sent = new();
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
        _mockUserService       = new Mock<IUserInterface>();

        // Every notification goes through INotificationService (stored + pushed); remember what was sent.
        _mockBell
            .Setup(b => b.SendNotificationAsync(It.IsAny<CreateNotificationInputDto>()))
            .Callback<CreateNotificationInputDto>(dto => _sent.Add(dto))
            .ReturnsAsync(new Lander.src.Notifications.Dtos.Dto.NotificationDto());

        // Default apartment stub
        _mockApartmentService
            .Setup(x => x.GetApartmentByIdAsync(ApartmentId))
            .ReturnsAsync(new GetApartmentDto { ApartmentId = ApartmentId, Title = "Test Apartment", LandlordId = LandlordUserId });

        // Default apartment list for landlord
        _mockApartmentService
            .Setup(x => x.GetApartmentsByLandlordIdAsync(LandlordUserId))
            .ReturnsAsync(new List<ApartmentDto> { new() { ApartmentId = ApartmentId, Title = "Test Apartment" } });

        var listingsOptions = new DbContextOptionsBuilder<ListingsContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var usersOptions = new DbContextOptionsBuilder<UsersContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var listingsContext = new ListingsContext(listingsOptions);
        var usersContext    = new UsersContext(usersOptions);

        // GetLandlordApplicationsAsync queries ListingsContext directly —
        // seed the landlord's apartment so ownership resolution works.
        listingsContext.Apartments.Add(new Apartment
        {
            ApartmentId = ApartmentId,
            LandlordId  = LandlordUserId,
            Title       = "Test Apartment",
            Address     = "Test Address 1",
            Rent        = 500,
            Features    = "{}",
            // Global query filter requires IsActive && !IsDeleted; the InMemory
            // provider ignores HasDefaultValue so set it explicitly.
            IsActive    = true,
        });
        listingsContext.SaveChanges();

        usersContext.Users.AddRange(
            new User { UserId = LandlordUserId, UserGuid = Guid.NewGuid(), FirstName = "Lana", LastName = "Stanodavac", Email = "lana@test.com", Password = "x", IsActive = true },
            new User { UserId = TenantUserId, UserGuid = Guid.NewGuid(), FirstName = "Tara", LastName = "Stanar", Email = "tara@test.com", Password = "x", IsActive = true });
        usersContext.SaveChanges();

        var mockHttp = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        mockHttp.Setup(x => x.HttpContext).Returns(new Microsoft.AspNetCore.Http.DefaultHttpContext());

        _service = new ApartmentApplicationService(
            _context,
            listingsContext,
            usersContext,
            _mockApartmentService.Object,
            _mockBell.Object,
            _mockUserService.Object,
            mockHttp.Object,
            _mockEmail.Object,
            _mockPresence.Object,
            new Mock<ILogger<ApartmentApplicationService>>().Object);
    }

    private async Task<ApartmentApplication> SeedApplicationAsync(string status)
    {
        var app = new ApartmentApplication
        {
            UserId = TenantUserId, ApartmentId = ApartmentId, Status = status,
            ApplicationDate = DateTime.UtcNow, CreatedDate = DateTime.UtcNow, CreatedByGuid = Guid.NewGuid(),
        };
        _context.ApartmentApplications.Add(app);
        await _context.SaveChangesAsync();
        return app;
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
        await _service.ApplyForApartmentAsync(TenantUserId, ApartmentId);

        _sent.Should().ContainSingle(n => n.RecipientUserId == LandlordUserId);
    }

    [Fact]
    public async Task ApplyForApartmentAsync_TheNotificationNamesTheApplicantAndTheApartmentInSerbian()
    {
        await _service.ApplyForApartmentAsync(TenantUserId, ApartmentId);

        var n = _sent.Single();
        n.Title.Should().Be("Nova prijava za vaš stan");
        n.Message.Should().Contain("Tara Stanar").And.Contain("Test Apartment");
    }

    [Fact]
    public async Task ApplyForApartmentAsync_TheNotificationIsTranslatableAndOpensTheReceivedApplications()
    {
        await _service.ApplyForApartmentAsync(TenantUserId, ApartmentId);

        var n = _sent.Single();
        n.Key.Should().Be("application_new");
        n.Params.Should().Contain("name", "Tara Stanar").And.Contain("title", "Test Apartment");
        n.ActionTarget.Should().Be("/applications/received");
        n.SenderUserId.Should().Be(TenantUserId);
    }

    [Fact]
    public async Task ApplyForApartmentAsync_WhenTheNotificationCannotBeSent_TheApplicationIsStillSaved()
    {
        _mockBell.Setup(b => b.SendNotificationAsync(It.IsAny<CreateNotificationInputDto>()))
            .ThrowsAsync(new InvalidOperationException("hub down"));

        var result = await _service.ApplyForApartmentAsync(TenantUserId, ApartmentId);

        result.Should().NotBeNull();
        (await _context.ApartmentApplications.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ApplyForApartmentAsync_LandlordIsAway_GetsAnEmailToo()
    {
        _mockPresence.Setup(p => p.IsOnline(LandlordUserId)).Returns(false);

        await _service.ApplyForApartmentAsync(TenantUserId, ApartmentId);

        _mockEmail.Verify(e => e.SendNewApplicationEmailAsync("lana@test.com", "Lana", "Test Apartment"), Times.Once);
    }

    [Fact]
    public async Task ApplyForApartmentAsync_LandlordIsOnTheSite_NoEmailJustTheLiveNotification()
    {
        _mockPresence.Setup(p => p.IsOnline(LandlordUserId)).Returns(true);

        await _service.ApplyForApartmentAsync(TenantUserId, ApartmentId);

        _mockEmail.Verify(e => e.SendNewApplicationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _sent.Should().ContainSingle(n => n.RecipientUserId == LandlordUserId);
    }

    [Fact]
    public async Task ApplyForApartmentAsync_WhenTheEmailFails_TheApplicationIsStillSaved()
    {
        _mockEmail.Setup(e => e.SendNewApplicationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("mail down"));

        var result = await _service.ApplyForApartmentAsync(TenantUserId, ApartmentId);

        result.Should().NotBeNull();
        (await _context.ApartmentApplications.CountAsync()).Should().Be(1);
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
    public async Task UpdateApplicationStatusAsync_Approved_TellsTheTenantAndLinksToTheApartment()
    {
        var app = await SeedApplicationAsync("Pending");

        await _service.UpdateApplicationStatusAsync(app.ApplicationId, "Approved", LandlordUserId);

        var n = _sent.Single(x => x.RecipientUserId == TenantUserId);
        n.Key.Should().Be("application_approved");
        n.Type.Should().Be("success");
        n.Params.Should().Contain("title", "Test Apartment");
        n.ActionTarget.Should().Be($"/apartments/{ApartmentId}");
        n.Message.Should().Contain("odobrena");
    }

    [Fact]
    public async Task UpdateApplicationStatusAsync_Rejected_SendsTheRejectionTypeWithTheDataTheDialogNeeds()
    {
        var app = await SeedApplicationAsync("Pending");

        await _service.UpdateApplicationStatusAsync(app.ApplicationId, "Rejected", LandlordUserId);

        var n = _sent.Single(x => x.RecipientUserId == TenantUserId);
        n.Key.Should().Be("application_rejected");
        n.Type.Should().Be("rejection");
        n.Metadata.Should().Contain("Test Apartment").And.Contain($"\"apartmentId\":{ApartmentId}");
        n.ActionTarget.Should().Be("/applications/sent");
    }

    [Theory]
    [InlineData("Pending", "application_pending")]
    [InlineData("Cancelled", "application_cancelled")]
    [InlineData("Completed", "application_status")]
    public async Task UpdateApplicationStatusAsync_OtherStatuses_UseTheirOwnKeyOrTheGenericOne(string status, string key)
    {
        var app = await SeedApplicationAsync("Approved");

        await _service.UpdateApplicationStatusAsync(app.ApplicationId, status, LandlordUserId);

        var n = _sent.Single(x => x.RecipientUserId == TenantUserId);
        n.Key.Should().Be(key);
        n.Params.Should().Contain("status", status);
        n.Type.Should().Be("info");
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
