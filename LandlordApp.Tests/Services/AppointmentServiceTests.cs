using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Lander;
using Lander.src.Modules.Appointments;
using Lander.src.Modules.Appointments.Implementation;
using Lander.src.Modules.Appointments.Models;
using Lander.src.Modules.Appointments.Dtos;
using Lander.src.Modules.Listings.Models;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Modules.Communication.Intefaces;
using Lander.src.Modules.ApartmentApplications.Interfaces;

namespace LandlordApp.Tests.Services;

public class AppointmentServiceTests : IDisposable
{
    private readonly AppointmentsContext _appointmentsContext;
    private readonly ListingsContext _listingsContext;
    private readonly UsersContext _usersContext;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ILogger<AppointmentService>> _mockLogger;
    private readonly Mock<IApplicationApprovalService> _mockApprovalService;
    private readonly AppointmentService _appointmentService;
    
    private readonly int _testTenantId = 1;
    private readonly Guid _testTenantGuid = Guid.NewGuid();
    private readonly int _testLandlordId = 2;
    private readonly Guid _testLandlordGuid = Guid.NewGuid();
    private readonly int _testApartmentId = 1;

    public AppointmentServiceTests()
    {
        // Setup in-memory databases
        var appointmentsOptions = new DbContextOptionsBuilder<AppointmentsContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var listingsOptions = new DbContextOptionsBuilder<ListingsContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var usersOptions = new DbContextOptionsBuilder<UsersContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _appointmentsContext = new AppointmentsContext(appointmentsOptions);
        _listingsContext = new ListingsContext(listingsOptions);
        _usersContext = new UsersContext(usersOptions);

        // Setup mocks
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<AppointmentService>>();
        _mockApprovalService = new Mock<IApplicationApprovalService>();

        // Default: approval check passes (tenant has approved application)
        _mockApprovalService
            .Setup(x => x.HasApprovedApplicationAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(true);

        // Setup default user context (tenant)
        SetupUserContext(_testTenantId, _testTenantGuid);

        // Create service instance
        _appointmentService = new AppointmentService(
            _appointmentsContext,
            _listingsContext,
            _usersContext,
            _mockEmailService.Object,
            _mockHttpContextAccessor.Object,
            _mockLogger.Object,
            _mockApprovalService.Object
        );

        // Seed test data
        SeedTestData().Wait();
    }

    private async Task SeedTestData()
    {
        // Create test apartment
        var apartment = new Apartment
        {
            ApartmentId = _testApartmentId,
            Title = "Test Apartment",
            Rent = 500,
            Address = "Test Address",
            City = "Beograd",
            LandlordId = _testLandlordId,
            IsActive = true,
            ListingType = ListingType.Rent
        };
        _listingsContext.Apartments.Add(apartment);
        await _listingsContext.SaveChangesAsync();

        // Create test users
        var tenant = new User
        {
            UserId = _testTenantId,
            UserGuid = _testTenantGuid,
            FirstName = "Tenant",
            LastName = "User",
            Email = "tenant@test.com",
            Password = "hashed",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var landlord = new User
        {
            UserId = _testLandlordId,
            UserGuid = _testLandlordGuid,
            FirstName = "Landlord",
            LastName = "User",
            Email = "landlord@test.com",
            Password = "hashed",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        _usersContext.Users.AddRange(tenant, landlord);
        await _usersContext.SaveChangesAsync();
    }

    private void SetupUserContext(int userId, Guid userGuid)
    {
        var claims = new List<Claim>
        {
            new Claim("userId", userId.ToString()),
            new Claim("sub", userGuid.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userGuid.ToString()) // Required by GetCurrentUserGuid()
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
    }

    public void Dispose()
    {
        _appointmentsContext.Database.EnsureDeleted();
        _listingsContext.Database.EnsureDeleted();
        _usersContext.Database.EnsureDeleted();
        _appointmentsContext.Dispose();
        _listingsContext.Dispose();
        _usersContext.Dispose();
    }

    #region CreateAppointmentAsync Tests

    [Fact]
    public async Task CreateAppointmentAsync_ValidInput_ShouldCreateAppointment()
    {
        // Arrange
        var futureDate = DateTime.Now.AddDays(2);
        var dto = new CreateAppointmentDto
        {
            ApartmentId = _testApartmentId,
            AppointmentDate = futureDate,
            TenantNotes = "Looking forward to viewing the apartment"
        };

        // Act
        var result = await _appointmentService.CreateAppointmentAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.ApartmentId.Should().Be(_testApartmentId);
        result.TenantId.Should().Be(_testTenantId);
        result.LandlordId.Should().Be(_testLandlordId);
        result.Status.Should().Be(AppointmentStatus.Pending);
        result.TenantNotes.Should().Be("Looking forward to viewing the apartment");

        var appointmentInDb = await _appointmentsContext.Appointments
            .FirstOrDefaultAsync(a => a.ApartmentId == _testApartmentId);
        appointmentInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAppointmentAsync_PastDate_ShouldThrowException()
    {
        // Arrange
        var pastDate = DateTime.Now.AddDays(-1);
        var dto = new CreateAppointmentDto
        {
            ApartmentId = _testApartmentId,
            AppointmentDate = pastDate
        };

        // Act
        var act = async () => await _appointmentService.CreateAppointmentAsync(dto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Appointment date must be in the future");
    }

    [Fact]
    public async Task CreateAppointmentAsync_NonExistentApartment_ShouldThrowException()
    {
        // Arrange
        var dto = new CreateAppointmentDto
        {
            ApartmentId = 99999,
            AppointmentDate = DateTime.Now.AddDays(1)
        };

        // Act
        var act = async () => await _appointmentService.CreateAppointmentAsync(dto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Apartment not found");
    }

    [Fact]
    public async Task CreateAppointmentAsync_ConflictingTimeSlot_ShouldThrowException()
    {
        // Arrange
        var appointmentDate = DateTime.Now.AddDays(3);
        
        // Create first appointment
        var existingAppointment = new Appointment
        {
            AppointmentGuid = Guid.NewGuid(),
            ApartmentId = _testApartmentId,
            TenantId = 999,
            LandlordId = _testLandlordId,
            AppointmentDate = appointmentDate,
            Status = AppointmentStatus.Confirmed,
            CreatedDate = DateTime.UtcNow
        };
        _appointmentsContext.Appointments.Add(existingAppointment);
        await _appointmentsContext.SaveChangesAsync();

        // Try to create conflicting appointment
        var dto = new CreateAppointmentDto
        {
            ApartmentId = _testApartmentId,
            AppointmentDate = appointmentDate
        };

        // Act
        var act = async () => await _appointmentService.CreateAppointmentAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("This time slot is already booked");
    }

    [Fact]
    public async Task CreateAppointmentAsync_ShouldSendEmailToLandlord()
    {
        // Arrange
        var futureDate = DateTime.Now.AddDays(2);
        var dto = new CreateAppointmentDto
        {
            ApartmentId = _testApartmentId,
            AppointmentDate = futureDate
        };

        _mockEmailService
            .Setup(x => x.SendAppointmentConfirmationEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        await _appointmentService.CreateAppointmentAsync(dto);

        // Assert
        _mockEmailService.Verify(
            x => x.SendAppointmentConfirmationEmailAsync(
                "landlord@test.com",
                "Landlord",
                futureDate,
                "Test Apartment"),
            Times.Once);
    }

    #endregion

    #region GetAvailableSlotsAsync Tests

    [Fact]
    public async Task GetAvailableSlotsAsync_ValidDate_ShouldReturnSlots()
    {
        // Arrange
        var futureDate = DateTime.Now.AddDays(5).Date;

        // Act
        var result = await _appointmentService.GetAvailableSlotsAsync(_testApartmentId, futureDate);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Should().OnlyContain(slot => slot.StartTime.Date == futureDate);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_WithBookedSlots_ShouldMarkAsUnavailable()
    {
        // Arrange
        var futureDate = DateTime.Now.AddDays(5).Date;
        var bookedTime = new DateTime(futureDate.Year, futureDate.Month, futureDate.Day, 10, 0, 0);

        var appointment = new Appointment
        {
            AppointmentGuid = Guid.NewGuid(),
            ApartmentId = _testApartmentId,
            TenantId = _testTenantId,
            LandlordId = _testLandlordId,
            AppointmentDate = bookedTime,
            Status = AppointmentStatus.Confirmed,
            CreatedDate = DateTime.UtcNow
        };
        _appointmentsContext.Appointments.Add(appointment);
        await _appointmentsContext.SaveChangesAsync();

        // Act
        var result = await _appointmentService.GetAvailableSlotsAsync(_testApartmentId, futureDate);

        // Assert
        result.Should().NotBeNull();
        var bookedSlot = result.FirstOrDefault(s => s.StartTime == bookedTime);
        bookedSlot.Should().NotBeNull();
        bookedSlot!.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_CancelledAppointment_ShouldShowAsAvailable()
    {
        // Arrange
        var futureDate = DateTime.Now.AddDays(5).Date;
        var cancelledTime = new DateTime(futureDate.Year, futureDate.Month, futureDate.Day, 11, 0, 0);

        var appointment = new Appointment
        {
            AppointmentGuid = Guid.NewGuid(),
            ApartmentId = _testApartmentId,
            TenantId = _testTenantId,
            LandlordId = _testLandlordId,
            AppointmentDate = cancelledTime,
            Status = AppointmentStatus.Cancelled,
            CreatedDate = DateTime.UtcNow
        };
        _appointmentsContext.Appointments.Add(appointment);
        await _appointmentsContext.SaveChangesAsync();

        // Act
        var result = await _appointmentService.GetAvailableSlotsAsync(_testApartmentId, futureDate);

        // Assert
        var slot = result.FirstOrDefault(s => s.StartTime == cancelledTime);
        slot.Should().NotBeNull();
        slot!.IsAvailable.Should().BeTrue(); // Cancelled slots should be available
    }

    #endregion

    #region UpdateAppointmentStatusAsync Tests

    [Fact]
    public async Task UpdateAppointmentStatusAsync_LandlordConfirms_ShouldUpdateStatus()
    {
        // Arrange
        var appointment = await CreateTestAppointment();
        SetupUserContext(_testLandlordId, _testLandlordGuid); // Switch to landlord

        var updateDto = new UpdateAppointmentStatusDto
        {
            Status = AppointmentStatus.Confirmed,
            LandlordNotes = "Looking forward to meeting you"
        };

        // Act
        var result = await _appointmentService.UpdateAppointmentStatusAsync(appointment.AppointmentId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(AppointmentStatus.Confirmed);
        result.LandlordNotes.Should().Be("Looking forward to meeting you");
    }

    [Fact]
    public async Task UpdateAppointmentStatusAsync_LandlordRejects_ShouldUpdateStatus()
    {
        // Arrange
        var appointment = await CreateTestAppointment();
        SetupUserContext(_testLandlordId, _testLandlordGuid);

        var updateDto = new UpdateAppointmentStatusDto
        {
            Status = AppointmentStatus.Rejected,
            LandlordNotes = "Sorry, not available at this time"
        };

        // Act
        var result = await _appointmentService.UpdateAppointmentStatusAsync(appointment.AppointmentId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(AppointmentStatus.Rejected);
    }

    [Fact]
    public async Task UpdateAppointmentStatusAsync_TenantTries_ShouldThrowUnauthorized()
    {
        // Arrange
        var appointment = await CreateTestAppointment();
        SetupUserContext(_testTenantId, _testTenantGuid); // Tenant trying to update

        var updateDto = new UpdateAppointmentStatusDto
        {
            Status = AppointmentStatus.Confirmed
        };

        // Act
        var act = async () => await _appointmentService.UpdateAppointmentStatusAsync(appointment.AppointmentId, updateDto);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Only the landlord can update appointment status");
    }

    [Fact]
    public async Task UpdateAppointmentStatusAsync_NonExistentAppointment_ShouldThrowException()
    {
        // Arrange
        SetupUserContext(_testLandlordId, _testLandlordGuid);
        var updateDto = new UpdateAppointmentStatusDto { Status = AppointmentStatus.Confirmed };

        // Act
        var act = async () => await _appointmentService.UpdateAppointmentStatusAsync(99999, updateDto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Appointment not found");
    }

    #endregion

    #region CancelAppointmentAsync Tests

    [Fact]
    public async Task CancelAppointmentAsync_TenantCancels_ShouldCancelAppointment()
    {
        // Arrange
        var appointment = await CreateTestAppointment();
        SetupUserContext(_testTenantId, _testTenantGuid);

        // Act
        var result = await _appointmentService.CancelAppointmentAsync(appointment.AppointmentId);

        // Assert
        result.Should().BeTrue();
        
        var cancelledAppointment = await _appointmentsContext.Appointments
            .FirstOrDefaultAsync(a => a.AppointmentId == appointment.AppointmentId);
        cancelledAppointment!.Status.Should().Be(AppointmentStatus.Cancelled);
    }

    [Fact]
    public async Task CancelAppointmentAsync_LandlordCancels_ShouldCancelAppointment()
    {
        // Arrange
        var appointment = await CreateTestAppointment();
        SetupUserContext(_testLandlordId, _testLandlordGuid);

        // Act
        var result = await _appointmentService.CancelAppointmentAsync(appointment.AppointmentId);

        // Assert
        result.Should().BeTrue();
        
        var cancelledAppointment = await _appointmentsContext.Appointments
            .FirstOrDefaultAsync(a => a.AppointmentId == appointment.AppointmentId);
        cancelledAppointment!.Status.Should().Be(AppointmentStatus.Cancelled);
    }

    [Fact]
    public async Task CancelAppointmentAsync_UnauthorizedUser_ShouldThrowException()
    {
        // Arrange
        var appointment = await CreateTestAppointment();
        SetupUserContext(999, Guid.NewGuid()); // Different user

        // Act
        var act = async () => await _appointmentService.CancelAppointmentAsync(appointment.AppointmentId);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    #endregion

    #region GetMyAppointmentsAsync Tests

    [Fact]
    public async Task GetMyAppointmentsAsync_Tenant_ShouldReturnTenantAppointments()
    {
        // Arrange
        await CreateTestAppointment();
        await CreateTestAppointment();
        SetupUserContext(_testTenantId, _testTenantGuid);

        // Act
        var result = await _appointmentService.GetMyAppointmentsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThan(0);
        result.Should().OnlyContain(a => a.TenantId == _testTenantId);
    }

    [Fact]
    public async Task GetMyAppointmentsAsync_NoAppointments_ShouldReturnEmpty()
    {
        // Arrange
        SetupUserContext(999, Guid.NewGuid()); // User with no appointments

        // Act
        var result = await _appointmentService.GetMyAppointmentsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region GetLandlordAppointmentsAsync Tests

    [Fact]
    public async Task GetLandlordAppointmentsAsync_ShouldReturnLandlordAppointments()
    {
        // Arrange
        await CreateTestAppointment();
        await CreateTestAppointment();
        SetupUserContext(_testLandlordId, _testLandlordGuid);

        // Act
        var result = await _appointmentService.GetLandlordAppointmentsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThan(0);
        result.Should().OnlyContain(a => a.LandlordId == _testLandlordId);
    }

    #endregion

    #region GetMyAvailabilityAsync Tests

    [Fact]
    public async Task GetMyAvailabilityAsync_NoSlotsSet_ShouldReturnEmptyList()
    {
        // Arrange — landlord has no availability rows yet
        SetupUserContext(_testLandlordId, _testLandlordGuid);

        // Act
        var result = await _appointmentService.GetMyAvailabilityAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyAvailabilityAsync_WithSlotsSet_ShouldReturnCorrectSlots()
    {
        // Arrange
        SetupUserContext(_testLandlordId, _testLandlordGuid);

        _appointmentsContext.LandlordAvailabilities.AddRange(
            new LandlordAvailability
            {
                LandlordId = _testLandlordId,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime   = new TimeSpan(12, 0, 0),
                IsActive  = true,
                CreatedDate = DateTime.UtcNow
            },
            new LandlordAvailability
            {
                LandlordId = _testLandlordId,
                DayOfWeek = DayOfWeek.Tuesday,
                StartTime = new TimeSpan(14, 0, 0),
                EndTime   = new TimeSpan(17, 0, 0),
                IsActive  = true,
                CreatedDate = DateTime.UtcNow
            },
            // A different landlord's slot – must NOT be returned
            new LandlordAvailability
            {
                LandlordId = 999,
                DayOfWeek = DayOfWeek.Wednesday,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime   = new TimeSpan(16, 0, 0),
                IsActive  = true,
                CreatedDate = DateTime.UtcNow
            }
        );
        await _appointmentsContext.SaveChangesAsync();

        // Act
        var result = await _appointmentService.GetMyAvailabilityAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.LandlordId == _testLandlordId);
        result.Should().ContainSingle(r => r.DayOfWeek == DayOfWeek.Monday);
        result.Should().ContainSingle(r => r.DayOfWeek == DayOfWeek.Tuesday);
    }

    [Fact]
    public async Task GetMyAvailabilityAsync_InactiveSlots_ShouldNotBeReturned()
    {
        // Arrange
        SetupUserContext(_testLandlordId, _testLandlordGuid);

        _appointmentsContext.LandlordAvailabilities.Add(new LandlordAvailability
        {
            LandlordId = _testLandlordId,
            DayOfWeek  = DayOfWeek.Friday,
            StartTime  = new TimeSpan(9, 0, 0),
            EndTime    = new TimeSpan(17, 0, 0),
            IsActive   = false, // inactive
            CreatedDate = DateTime.UtcNow
        });
        await _appointmentsContext.SaveChangesAsync();

        // Act
        var result = await _appointmentService.GetMyAvailabilityAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region SetMyAvailabilityAsync Tests

    [Fact]
    public async Task SetMyAvailabilityAsync_NewSlots_ShouldPersistAll()
    {
        // Arrange
        SetupUserContext(_testLandlordId, _testLandlordGuid);

        var dto = new SetAvailabilityDto
        {
            Slots = new List<AvailabilitySlotInput>
            {
                new() { DayOfWeek = DayOfWeek.Monday,    StartTime = new TimeSpan(9, 0, 0),  EndTime = new TimeSpan(12, 0, 0) },
                new() { DayOfWeek = DayOfWeek.Wednesday, StartTime = new TimeSpan(13, 0, 0), EndTime = new TimeSpan(17, 0, 0) },
            }
        };

        // Act
        var result = await _appointmentService.SetMyAvailabilityAsync(dto);

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainSingle(r => r.DayOfWeek == DayOfWeek.Monday);
        result.Should().ContainSingle(r => r.DayOfWeek == DayOfWeek.Wednesday);

        var dbSlots = await _appointmentsContext.LandlordAvailabilities
            .Where(la => la.LandlordId == _testLandlordId)
            .ToListAsync();
        dbSlots.Should().HaveCount(2);
    }

    [Fact]
    public async Task SetMyAvailabilityAsync_ReplacesExistingSlots()
    {
        // Arrange
        SetupUserContext(_testLandlordId, _testLandlordGuid);

        // Seed old slots
        _appointmentsContext.LandlordAvailabilities.AddRange(
            new LandlordAvailability
            {
                LandlordId = _testLandlordId, DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0),
                IsActive = true, CreatedDate = DateTime.UtcNow
            },
            new LandlordAvailability
            {
                LandlordId = _testLandlordId, DayOfWeek = DayOfWeek.Tuesday,
                StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0),
                IsActive = true, CreatedDate = DateTime.UtcNow
            }
        );
        await _appointmentsContext.SaveChangesAsync();

        var newDto = new SetAvailabilityDto
        {
            Slots = new List<AvailabilitySlotInput>
            {
                new() { DayOfWeek = DayOfWeek.Friday, StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(14, 0, 0) }
            }
        };

        // Act
        var result = await _appointmentService.SetMyAvailabilityAsync(newDto);

        // Assert — only the new slot should exist
        result.Should().HaveCount(1);
        result.First().DayOfWeek.Should().Be(DayOfWeek.Friday);

        var dbSlots = await _appointmentsContext.LandlordAvailabilities
            .Where(la => la.LandlordId == _testLandlordId)
            .ToListAsync();
        dbSlots.Should().HaveCount(1);
        dbSlots.First().DayOfWeek.Should().Be(DayOfWeek.Friday);
    }

    [Fact]
    public async Task SetMyAvailabilityAsync_EmptyList_ClearsAllSlots()
    {
        // Arrange
        SetupUserContext(_testLandlordId, _testLandlordGuid);

        _appointmentsContext.LandlordAvailabilities.Add(new LandlordAvailability
        {
            LandlordId = _testLandlordId, DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0),
            IsActive = true, CreatedDate = DateTime.UtcNow
        });
        await _appointmentsContext.SaveChangesAsync();

        var dto = new SetAvailabilityDto { Slots = new List<AvailabilitySlotInput>() };

        // Act
        var result = await _appointmentService.SetMyAvailabilityAsync(dto);

        // Assert
        result.Should().BeEmpty();

        var dbSlots = await _appointmentsContext.LandlordAvailabilities
            .Where(la => la.LandlordId == _testLandlordId)
            .ToListAsync();
        dbSlots.Should().BeEmpty();
    }

    #endregion

    #region GetAvailableSlots with LandlordAvailability Tests

    [Fact]
    public async Task GetAvailableSlotsAsync_NoAvailabilitySet_ShouldUseFallback9To17()
    {
        // Arrange — no LandlordAvailability rows seeded
        var futureDate = GetNextWeekday(DayOfWeek.Monday);

        // Act
        var result = await _appointmentService.GetAvailableSlotsAsync(_testApartmentId, futureDate);

        // Assert: fallback 9-17 should produce slots starting at 09:00
        result.Should().NotBeEmpty();
        result.Min(s => s.StartTime.Hour).Should().Be(9);
        result.Max(s => s.StartTime.Hour).Should().BeLessThan(17);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_WithLandlordAvailability_ShouldRespectCustomWindow()
    {
        // Arrange — landlord only available 14:00–16:00 on Tuesdays
        var tuesday = GetNextWeekday(DayOfWeek.Tuesday);

        _appointmentsContext.LandlordAvailabilities.Add(new LandlordAvailability
        {
            LandlordId = _testLandlordId,
            DayOfWeek  = DayOfWeek.Tuesday,
            StartTime  = new TimeSpan(14, 0, 0),
            EndTime    = new TimeSpan(16, 0, 0),
            IsActive   = true,
            CreatedDate = DateTime.UtcNow
        });
        await _appointmentsContext.SaveChangesAsync();

        // Act
        var result = await _appointmentService.GetAvailableSlotsAsync(_testApartmentId, tuesday);

        // Assert: no slots before 14:00 or at/after 16:00
        result.Should().NotBeEmpty();
        result.Should().OnlyContain(s => s.StartTime.Hour >= 14 && s.StartTime.Hour < 16);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_WithLandlordAvailability_BookedSlotMarkedUnavailable()
    {
        // Arrange
        var wednesday = GetNextWeekday(DayOfWeek.Wednesday);
        var bookedTime = new DateTime(wednesday.Year, wednesday.Month, wednesday.Day, 10, 0, 0);

        _appointmentsContext.LandlordAvailabilities.Add(new LandlordAvailability
        {
            LandlordId = _testLandlordId,
            DayOfWeek  = DayOfWeek.Wednesday,
            StartTime  = new TimeSpan(9, 0, 0),
            EndTime    = new TimeSpan(13, 0, 0),
            IsActive   = true,
            CreatedDate = DateTime.UtcNow
        });

        _appointmentsContext.Appointments.Add(new Appointment
        {
            AppointmentGuid = Guid.NewGuid(),
            ApartmentId     = _testApartmentId,
            TenantId        = _testTenantId,
            LandlordId      = _testLandlordId,
            AppointmentDate = bookedTime,
            Status          = AppointmentStatus.Confirmed,
            CreatedDate     = DateTime.UtcNow
        });
        await _appointmentsContext.SaveChangesAsync();

        // Act
        var result = await _appointmentService.GetAvailableSlotsAsync(_testApartmentId, wednesday);

        // Assert
        var bookedSlot = result.FirstOrDefault(s => s.StartTime == bookedTime);
        bookedSlot.Should().NotBeNull();
        bookedSlot!.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAppointmentStatusAsync_AlreadyConfirmed_ShouldRemainConfirmed()
    {
        // Arrange
        var appointment = await CreateTestAppointment();
        appointment.Status = AppointmentStatus.Confirmed;
        await _appointmentsContext.SaveChangesAsync();

        SetupUserContext(_testLandlordId, _testLandlordGuid);

        var updateDto = new UpdateAppointmentStatusDto
        {
            Status = AppointmentStatus.Confirmed,
            LandlordNotes = "Still confirmed"
        };

        // Act
        var result = await _appointmentService.UpdateAppointmentStatusAsync(appointment.AppointmentId, updateDto);

        // Assert
        result.Status.Should().Be(AppointmentStatus.Confirmed);
        result.LandlordNotes.Should().Be("Still confirmed");
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_NoActiveAvailability_ShouldFallback()
    {
        // Arrange
        var monday = GetNextWeekday(DayOfWeek.Monday);
        
        // Add only inactive slots
        _appointmentsContext.LandlordAvailabilities.Add(new LandlordAvailability
        {
            LandlordId = _testLandlordId,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(12, 0, 0),
            IsActive = false
        });
        await _appointmentsContext.SaveChangesAsync();

        // Act
        var result = await _appointmentService.GetAvailableSlotsAsync(_testApartmentId, monday);

        // Assert: Should fallback to 9:00 - 17:00
        result.Should().NotBeEmpty();
        result.Min(s => s.StartTime.Hour).Should().Be(9);
        result.Max(s => s.StartTime.Hour).Should().BeLessThan(17);
    }

    #endregion

    #region Helper Methods

    private async Task<Appointment> CreateTestAppointment()
    {
        var appointment = new Appointment
        {
            AppointmentGuid = Guid.NewGuid(),
            ApartmentId = _testApartmentId,
            TenantId = _testTenantId,
            LandlordId = _testLandlordId,
            AppointmentDate = DateTime.Now.AddDays(7),
            Duration = TimeSpan.FromMinutes(30),
            Status = AppointmentStatus.Pending,
            CreatedDate = DateTime.UtcNow,
            CreatedByGuid = _testTenantGuid
        };

        _appointmentsContext.Appointments.Add(appointment);
        await _appointmentsContext.SaveChangesAsync();
        return appointment;
    }

    /// <summary>Returns the next future date (at midnight) whose DayOfWeek matches <paramref name="dayOfWeek"/>.</summary>
    private static DateTime GetNextWeekday(DayOfWeek dayOfWeek)
    {
        var today = DateTime.Today;
        int daysUntil = ((int)dayOfWeek - (int)today.DayOfWeek + 7) % 7;
        if (daysUntil == 0) daysUntil = 7; // always pick a future date
        return today.AddDays(daysUntil);
    }

    #endregion
}
