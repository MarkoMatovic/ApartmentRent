using FluentAssertions;
using Lander;
using Lander.Helpers;
using Lander.src.Modules.Appointments;
using Lander.src.Modules.Appointments.Dtos;
using Lander.src.Modules.Appointments.Implementation;
using Lander.src.Modules.Appointments.Models;
using Lander.src.Modules.ApartmentApplications.Interfaces;
using Lander.src.Modules.Communication.Interfaces;
using Lander.src.Modules.Listings.Dtos.InputDto;
using Lander.src.Modules.Listings.Implementation;
using Lander.src.Modules.Listings.Interfaces;
using Lander.src.Modules.Listings.Models;
using Lander.src.Modules.Roommates.Interfaces;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Common;
using Lander.src.Modules.Users.Dtos.InputDto;
using Lander.src.Modules.Users.Implementation.UserImplementation;
using Lander.src.Notifications.NotificationsHub;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace LandlordApp.Tests.Services;

// ─────────────────────────────────────────────────────────────────────────────
// UserService — Account Lockout Edge Cases
// ─────────────────────────────────────────────────────────────────────────────
public class UserServiceLockoutTests : IDisposable
{
    private readonly UsersContext _context;
    private readonly UserService _userService;

    public UserServiceLockoutTests()
    {
        var opts = new DbContextOptionsBuilder<UsersContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _context = new UsersContext(opts);

        var reviewsOpts = new DbContextOptionsBuilder<ReviewsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var reviewsCtx = new ReviewsContext(reviewsOpts);

        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"]              = "SuperSecretTestKey12345678901234",
                ["Jwt:Issuer"]              = "TestIssuer",
                ["Jwt:Audience"]            = "TestAudience",
                ["App:FrontendBaseUrl"]     = "http://localhost:5173"
            }).Build();

        var tokenProvider = new TokenProvider(cfg, _context);

        // Seed roles
        _context.Roles.Add(new Role { RoleId = 1, RoleName = "Tenant", Description = "t", CreatedDate = DateTime.UtcNow });
        _context.SaveChanges();

        _userService = new UserService(
            _context, reviewsCtx, tokenProvider,
            new Mock<IHttpContextAccessor>().Object,
            new Mock<IEmailService>().Object,
            new Mock<IApartmentService>().Object,
            new Mock<IRoommateService>().Object,
            Array.Empty<Lander.src.Common.IUserDeletedHandler>(),
            new RefreshTokenService(_context),
            new Mock<ILogger<UserService>>().Object,
            cfg);
    }

    public void Dispose() => _context.Database.EnsureDeleted();

    private async Task<User> SeedLockedUser(string email = "locked@test.com")
    {
        var user = new User
        {
            FirstName = "Lock", LastName = "Test", Email = email,
            Password = BCrypt.Net.BCrypt.HashPassword("CorrectPass1"),
            IsActive = true, UserRoleId = 1,
            UserGuid = Guid.NewGuid(), CreatedDate = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task Login_WrongPassword_FiveAttempts_AccountLocked()
    {
        await SeedLockedUser("lockme@test.com");

        var dto = new LoginUserInputDto { Email = "lockme@test.com", Password = "WrongPass!" };

        // 5 failed attempts
        for (int i = 0; i < 5; i++)
            await _userService.LoginUserAsync(dto);

        var user = await _context.Users.FirstAsync(u => u.Email == "lockme@test.com");
        user.FailedLoginAttempts.Should().Be(5);
        user.LockoutUntil.Should().NotBeNull();
        user.LockoutUntil!.Value.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_LockedAccount_ReturnsNull()
    {
        var user = await SeedLockedUser("blocked@test.com");
        user.LockoutUntil = DateTime.UtcNow.AddMinutes(15);
        user.FailedLoginAttempts = 5;
        await _context.SaveChangesAsync();

        var result = await _userService.LoginUserAsync(
            new LoginUserInputDto { Email = "blocked@test.com", Password = "CorrectPass1" });

        result.Should().BeNull();
    }

    [Fact]
    public async Task Login_SuccessAfterFailures_ResetsLockoutCounters()
    {
        var user = await SeedLockedUser("reset@test.com");
        user.FailedLoginAttempts = 3;
        await _context.SaveChangesAsync();

        var result = await _userService.LoginUserAsync(
            new LoginUserInputDto { Email = "reset@test.com", Password = "CorrectPass1" });

        result.Should().NotBeNull();
        var updated = await _context.Users.FirstAsync(u => u.Email == "reset@test.com");
        updated.FailedLoginAttempts.Should().Be(0);
        updated.LockoutUntil.Should().BeNull();
    }

    [Fact]
    public async Task Register_DuplicateEmail_ThrowsException()
    {
        var dto = new UserRegistrationInputDto
        {
            FirstName = "A", LastName = "B",
            Email = "dup@test.com", Password = "Pass123!",
            DateOfBirth = new DateTime(1990, 1, 1)
        };
        await _userService.RegisterUserAsync(dto);

        Func<Task> act = () => _userService.RegisterUserAsync(dto);
        await act.Should().ThrowAsync<Exception>();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// AppointmentService — Time Conflict & Edge Cases
// ─────────────────────────────────────────────────────────────────────────────
public class AppointmentServiceConflictTests : IDisposable
{
    private readonly AppointmentsContext _appointments;
    private readonly ListingsContext _listings;
    private readonly UsersContext _users;
    private readonly AppointmentService _service;
    private readonly Mock<IHttpContextAccessor> _mockHttp;

    private const int TenantId   = 1;
    private const int LandlordId = 2;
    private const int AptId      = 10;
    private readonly Guid _tenantGuid = Guid.NewGuid();

    public AppointmentServiceConflictTests()
    {
        _appointments = new AppointmentsContext(new DbContextOptionsBuilder<AppointmentsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
        _listings = new ListingsContext(new DbContextOptionsBuilder<ListingsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
        _users = new UsersContext(new DbContextOptionsBuilder<UsersContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);

        _mockHttp = new Mock<IHttpContextAccessor>();
        SetupUserContext(TenantId, _tenantGuid);

        // Seed apartment and users
        _listings.Apartments.Add(new Apartment
        {
            ApartmentId = AptId, Title = "T", Address = "A", City = "C",
            LandlordId = LandlordId, IsActive = true, ListingType = ListingType.Rent
        });
        _listings.SaveChanges();
        _users.Users.AddRange(
            new User { UserId = TenantId,   UserGuid = _tenantGuid,    Email = "t@t.com", Password = "h", FirstName = "T", LastName = "T", IsActive = true, CreatedDate = DateTime.UtcNow },
            new User { UserId = LandlordId, UserGuid = Guid.NewGuid(), Email = "l@l.com", Password = "h", FirstName = "L", LastName = "L", IsActive = true, CreatedDate = DateTime.UtcNow }
        );
        _users.SaveChanges();

        var mockApproval = new Mock<IApplicationApprovalService>();
        mockApproval.Setup(x => x.HasApprovedApplicationAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(true);

        _service = new AppointmentService(
            _appointments, _listings, _users,
            new Mock<IEmailService>().Object,
            _mockHttp.Object,
            new Mock<ILogger<AppointmentService>>().Object,
            mockApproval.Object);
    }

    public void Dispose()
    {
        _appointments.Database.EnsureDeleted();
        _listings.Database.EnsureDeleted();
        _users.Database.EnsureDeleted();
    }

    private void SetupUserContext(int userId, Guid guid)
    {
        var claims = new List<Claim>
        {
            new("userId", userId.ToString()),
            new("sub", guid.ToString()),
            new(ClaimTypes.NameIdentifier, guid.ToString())
        };
        var httpCtx = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) };
        _mockHttp.Setup(x => x.HttpContext).Returns(httpCtx);
    }

    [Fact]
    public async Task CreateAppointment_TimeConflict_ThrowsInvalidOperation()
    {
        var futureDate = DateTime.Now.AddDays(3);

        // Dodaj prvi termin
        await _service.CreateAppointmentAsync(new CreateAppointmentDto
        {
            ApartmentId = AptId, AppointmentDate = futureDate
        });

        // Isti termin — konflikt
        Func<Task> act = () => _service.CreateAppointmentAsync(new CreateAppointmentDto
        {
            ApartmentId = AptId, AppointmentDate = futureDate
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already booked*");
    }

    [Fact]
    public async Task CreateAppointment_PastDate_ThrowsArgumentException()
    {
        Func<Task> act = () => _service.CreateAppointmentAsync(new CreateAppointmentDto
        {
            ApartmentId = AptId, AppointmentDate = DateTime.Now.AddDays(-1)
        });

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*future*");
    }

    [Fact]
    public async Task CreateAppointment_DifferentTimeSameApartment_BothSucceed()
    {
        var date1 = DateTime.Now.AddDays(4);
        var date2 = DateTime.Now.AddDays(5);

        await _service.CreateAppointmentAsync(new CreateAppointmentDto { ApartmentId = AptId, AppointmentDate = date1 });
        var result = await _service.CreateAppointmentAsync(new CreateAppointmentDto { ApartmentId = AptId, AppointmentDate = date2 });

        result.Should().NotBeNull();
        (await _appointments.Appointments.CountAsync()).Should().Be(2);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// ApartmentService — Features JSON Round-Trip (bug fix verifikacija)
// ─────────────────────────────────────────────────────────────────────────────
public class ApartmentServiceFeaturesTests : IDisposable
{
    private readonly ListingsContext _context;
    private readonly UsersContext _usersContext;
    private readonly ApartmentService _service;
    private readonly Guid _landlordGuid = Guid.NewGuid();
    private const int LandlordId = 1;

    public ApartmentServiceFeaturesTests()
    {
        _context = new ListingsContext(new DbContextOptionsBuilder<ListingsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
        _usersContext = new UsersContext(new DbContextOptionsBuilder<UsersContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
        var savedSearchesCtx = new SavedSearchesContext(new DbContextOptionsBuilder<SavedSearchesContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
        var reviewsCtx = new ReviewsContext(new DbContextOptionsBuilder<ReviewsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);

        _usersContext.Roles.Add(new Role { RoleId = 1, RoleName = "Landlord", Description = "l", CreatedDate = DateTime.UtcNow });
        _usersContext.Users.Add(new User { UserId = LandlordId, UserGuid = _landlordGuid, Email = "ll@test.com", Password = "h", FirstName = "L", LastName = "L", IsActive = true, UserRoleId = 1, CreatedDate = DateTime.UtcNow });
        _usersContext.SaveChanges();

        var mockHttp = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim>
        {
            new("userId", LandlordId.ToString()),
            new("sub", _landlordGuid.ToString()),
            new(ClaimTypes.NameIdentifier, _landlordGuid.ToString())
        };
        mockHttp.Setup(x => x.HttpContext).Returns(new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
        });

        var mockCache = new Mock<IMemoryCache>();
        object? cacheVal = null;
        mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheVal)).Returns(false);
        mockCache.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>());

        // Setup NotificationHub mock — Clients.All.SendAsync mora biti validan
        var mockHubClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClientProxy.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockHubClients.Setup(c => c.All).Returns(mockClientProxy.Object);
        var mockHub = new Mock<IHubContext<NotificationHub>>();
        mockHub.Setup(h => h.Clients).Returns(mockHubClients.Object);

        _service = new ApartmentService(
            _context, _usersContext, savedSearchesCtx,
            new Mock<IEmailService>().Object,
            mockHttp.Object,
            mockCache.Object,
            mockHub.Object,
            reviewsCtx,
            new Mock<ILogger<ApartmentService>>().Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _usersContext.Database.EnsureDeleted();
    }

    [Fact]
    public async Task CreateAndGetById_IsFurnished_True_RoundTrips()
    {
        var created = await _service.CreateApartmentAsync(new ApartmentInputDto
        {
            Title = "Furnished Apt", Description = "desc", Address = "Addr", City = "NS",
            Rent = 400, ListingType = ListingType.Rent,
            IsFurnished = true, HasParking = true, HasBalcony = false
        });

        var detail = await _service.GetApartmentByIdAsync(created.ApartmentId);

        detail.Should().NotBeNull();
        detail!.IsFurnished.Should().BeTrue();
        detail.HasParking.Should().BeTrue();
        detail.HasBalcony.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAndGetById_AllFeaturesFalse_RoundTrips()
    {
        var created = await _service.CreateApartmentAsync(new ApartmentInputDto
        {
            Title = "Basic Apt", Description = "desc", Address = "Addr", City = "BG",
            Rent = 300, ListingType = ListingType.Rent,
            IsFurnished = false, HasParking = false, IsPetFriendly = false
        });

        var detail = await _service.GetApartmentByIdAsync(created.ApartmentId);

        detail!.IsFurnished.Should().BeFalse();
        detail.HasParking.Should().BeFalse();
        detail.IsPetFriendly.Should().BeFalse();
    }

    [Fact]
    public async Task GetApartmentById_NonExistent_ReturnsNull()
    {
        var result = await _service.GetApartmentByIdAsync(99999);
        result.Should().BeNull();
    }
}
