using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Lander;
using Lander.src.Modules.Users.Implementation.UserImplementation;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Modules.Users.Dtos.Dto;
using Lander.src.Modules.Users.Dtos.InputDto;
using Lander.Helpers;
using Lander.src.Modules.Communication.Interfaces;
using Lander.src.Modules.Listings.Interfaces;
using Lander.src.Modules.Listings.Dtos.Dto;
using Lander.src.Modules.Roommates.Interfaces;
using Lander.src.Modules.Roommates.Dtos.Dto;
using Lander.src.Common;

namespace LandlordApp.Tests.Services;

public class UserServiceTests : IDisposable
{
    private readonly UsersContext _context;
    private readonly ReviewsContext _reviewsContext;
    private readonly TokenProvider _tokenProvider;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IApartmentService> _mockApartmentService;
    private readonly Mock<IRoommateService> _mockRoommateService;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly UserService _userService;

    // Helper: creates a valid User with all required non-nullable fields
    private User MakeUser(int? userId = null, string email = "test@test.com", bool isActive = true, int? roleId = 1)
    {
        var u = new User
        {
            FirstName = "Test",
            LastName = "User",
            Email = email,
            Password = BCrypt.Net.BCrypt.HashPassword("password"),
            IsActive = isActive,
            UserRoleId = roleId,
            UserGuid = Guid.NewGuid()
        };
        if (userId.HasValue) u.UserId = userId.Value;
        return u;
    }

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<UsersContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var reviewsOptions = new DbContextOptionsBuilder<ReviewsContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new UsersContext(options);
        _reviewsContext = new ReviewsContext(reviewsOptions);

        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(x => x["Jwt:Secret"]).Returns("ThisIsAVerySecureSecretKeyForTestingPurposesOnly12345678");
        mockConfiguration.Setup(x => x["Jwt:Issuer"]).Returns("TestIssuer");
        mockConfiguration.Setup(x => x["Jwt:Audience"]).Returns("TestAudience");
        _tokenProvider = new TokenProvider(mockConfiguration.Object, _context);

        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockEmailService = new Mock<IEmailService>();
        _mockApartmentService = new Mock<IApartmentService>();
        _mockRoommateService = new Mock<IRoommateService>();
        _mockLogger = new Mock<ILogger<UserService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(x => x["Jwt:Secret"]).Returns("ThisIsAVerySecureSecretKeyForTestingPurposesOnly12345678");
        _mockConfiguration.Setup(x => x["Jwt:Issuer"]).Returns("TestIssuer");
        _mockConfiguration.Setup(x => x["Jwt:Audience"]).Returns("TestAudience");
        _mockConfiguration.Setup(x => x["App:FrontendBaseUrl"]).Returns("http://localhost:5173");
        var refreshTokenService = new RefreshTokenService(_context);

        _userService = new UserService(
            _context, _reviewsContext, _tokenProvider,
            _mockHttpContextAccessor.Object, _mockEmailService.Object,
            _mockApartmentService.Object, _mockRoommateService.Object,
            Array.Empty<IUserDeletedHandler>(),
            refreshTokenService, _mockLogger.Object, _mockConfiguration.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        _context.Roles.AddRange(
            new Role { RoleId = 1, RoleName = "Tenant",   Description = "Tenant role",   CreatedDate = DateTime.UtcNow },
            new Role { RoleId = 2, RoleName = "Landlord", Description = "Landlord role", CreatedDate = DateTime.UtcNow }
        );
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _reviewsContext.Database.EnsureDeleted();
        _context.Dispose();
        _reviewsContext.Dispose();
    }

    // ─── Registration & Login ────────────────────────────────────────────────

    [Fact]
    public async Task RegisterUserAsync_ValidInput_ShouldCreateUser()
    {
        var dto = new UserRegistrationInputDto
        {
            FirstName = "Ex", LastName = "User",
            Email = "ex@test.com", Password = "Pass123!",
            DateOfBirth = new DateTime(1990, 1, 1)
        };
        var result = await _userService.RegisterUserAsync(dto);
        result.FirstName.Should().Be("Ex");
        (await _context.Users.AnyAsync(u => u.Email == "ex@test.com")).Should().BeTrue();
    }

    [Fact]
    public async Task LoginUserAsync_ValidCredentials_ShouldReturnToken()
    {
        var user = MakeUser(email: "login@test.com");
        user.Password = BCrypt.Net.BCrypt.HashPassword("TestPassword123");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result = await _userService.LoginUserAsync(new LoginUserInputDto { Email = "login@test.com", Password = "TestPassword123" });
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginUserAsync_InactiveUser_ShouldReturnNull()
    {
        var user = MakeUser(email: "inactive@test.com", isActive: false);
        user.Password = BCrypt.Net.BCrypt.HashPassword("pass");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result = await _userService.LoginUserAsync(new LoginUserInputDto { Email = "inactive@test.com", Password = "pass" });
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginUserAsync_WrongPassword_ShouldReturnNull()
    {
        var user = MakeUser(email: "wrongpass@test.com");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result = await _userService.LoginUserAsync(new LoginUserInputDto { Email = "wrongpass@test.com", Password = "wrongpassword" });
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginUserAsync_NonExistentEmail_ShouldReturnNull()
    {
        var result = await _userService.LoginUserAsync(new LoginUserInputDto { Email = "nobody@test.com", Password = "pass" });
        result.Should().BeNull();
    }

    // ─── Profile Management ──────────────────────────────────────────────────

    [Fact]
    public async Task GetUserProfileAsync_ShouldReturnProfile()
    {
        var user = MakeUser(userId: 50, email: "rated@test.com");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result = await _userService.GetUserProfileAsync(50);
        result.Should().NotBeNull();
        result!.Email.Should().Be("rated@test.com");
    }

    [Fact]
    public async Task GetUserProfileAsync_WithReviews_ShouldAggregateRatings()
    {
        var user = MakeUser(userId: 51, email: "rateduser@test.com");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _reviewsContext.Reviews.AddRange(
            new Lander.src.Modules.Reviews.Modules.Review { LandlordId = 51, Rating = 5 },
            new Lander.src.Modules.Reviews.Modules.Review { TenantId  = 51, Rating = 3 }
        );
        await _reviewsContext.SaveChangesAsync();

        var result = await _userService.GetUserProfileAsync(51);
        result!.AverageRating.Should().Be(4);
        result.ReviewCount.Should().Be(2);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_FullUpdate_ShouldSucceed()
    {
        var user = MakeUser(email: "old@test.com");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var updateDto = new UserProfileUpdateInputDto { FirstName = "New", Email = "new@test.com" };
        var result = await _userService.UpdateUserProfileAsync(user.UserId, updateDto);
        result.FirstName.Should().Be("New");
        result.Email.Should().Be("new@test.com");
    }

    [Fact]
    public async Task UpdatePrivacySettingsAsync_ValidUpdate_ShouldPersist()
    {
        var user = MakeUser(email: "priv@test.com");
        user.AnalyticsConsent = false;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await _userService.UpdatePrivacySettingsAsync(user.UserId, new PrivacySettingsDto { AnalyticsConsent = true, ProfileVisibility = true });
        var dbUser = await _context.Users.FindAsync(user.UserId);
        dbUser!.AnalyticsConsent.Should().BeTrue();
        dbUser.ProfileVisibility.Should().BeTrue();
    }

    // ─── Role & Status ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpgradeUserRoleAsync_ValidRole_ShouldUpdate()
    {
        var user = MakeUser(userId: 10, email: "roletest@test.com");
        user.UserRoleId = 1;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await _userService.UpgradeUserRoleAsync(10, "Landlord");
        var dbUser = await _context.Users.FindAsync(10);
        dbUser!.UserRoleId.Should().Be(2);
    }

    [Fact]
    public async Task UpgradeUserRoleAsync_RoleNotFound_ShouldThrow()
    {
        var user = MakeUser(userId: 11, email: "badrole@test.com");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var act = async () => await _userService.UpgradeUserRoleAsync(11, "NonExistent");
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task UpdateRoommateStatusAsync_ShouldToggle()
    {
        var guid = Guid.NewGuid();
        var user = MakeUser(email: "roomie@test.com");
        user.UserGuid = guid;
        user.IsLookingForRoommate = false;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await _userService.UpdateRoommateStatusAsync(new UpdateRoommateStatusInputDto { UserGuid = guid, IsLookingForRoommate = true });
        var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.UserGuid == guid);
        dbUser!.IsLookingForRoommate.Should().BeTrue();
    }

    // ─── Export & Security ───────────────────────────────────────────────────

    [Fact]
    public async Task ExportUserDataAsync_ShouldAggregateModules()
    {
        var user = MakeUser(userId: 70, email: "export@test.com");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _mockApartmentService.Setup(s => s.GetApartmentsByLandlordIdAsync(70)).ReturnsAsync(new List<ApartmentDto> { new ApartmentDto() });
        _mockRoommateService.Setup(s => s.GetRoommateByUserIdAsync(70)).ReturnsAsync(new RoommateDto());

        var result = await _userService.ExportUserDataAsync(70);
        result.UserProfile.FirstName.Should().Be("Test");
        result.ListedApartments.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ChangePasswordAsync_Valid_ShouldUpdate()
    {
        var guid = Guid.NewGuid();
        var user = MakeUser(email: "chpass@test.com");
        user.UserGuid = guid;
        user.Password = BCrypt.Net.BCrypt.HashPassword("old");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        SetupUserContext(1, guid);

        await _userService.ChangePasswordAsync(new ChangePasswordInputDto { OldPassword = "old", NewPassword = "newpass" });
        var dbUser = await _context.Users.FirstAsync(u => u.UserGuid == guid);
        BCrypt.Net.BCrypt.Verify("newpass", dbUser.Password).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldCallHandlersAndRemoveUser()
    {
        var guid = Guid.NewGuid();
        var user = MakeUser(userId: 99, email: "del@test.com");
        user.UserGuid = guid;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var mockHandler = new Mock<IUserDeletedHandler>();
        mockHandler.Setup(h => h.HandleAsync(It.IsAny<int>())).Returns(Task.CompletedTask);

        var refreshTokenService = new RefreshTokenService(_context);
        var svc = new UserService(
            _context, _reviewsContext, _tokenProvider,
            _mockHttpContextAccessor.Object, _mockEmailService.Object,
            _mockApartmentService.Object, _mockRoommateService.Object,
            new[] { mockHandler.Object },
            refreshTokenService, _mockLogger.Object, _mockConfiguration.Object);

        await svc.DeleteUserAsync(new DeleteUserInputDto { UserGuid = guid });

        mockHandler.Verify(h => h.HandleAsync(99), Times.Once);
        (await _context.Users.AnyAsync(u => u.UserGuid == guid)).Should().BeFalse();
    }

    // ─── Account Lockout ─────────────────────────────────────────────────────

    [Fact]
    public async Task LoginUserAsync_WrongPassword_IncrementsFailedAttempts()
    {
        var user = MakeUser(email: "locktest@test.com");
        user.Password = BCrypt.Net.BCrypt.HashPassword("correct");
        user.FailedLoginAttempts = 0;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await _userService.LoginUserAsync(new LoginUserInputDto { Email = "locktest@test.com", Password = "wrong" });

        var dbUser = await _context.Users.FirstAsync(u => u.Email == "locktest@test.com");
        dbUser.FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public async Task LoginUserAsync_FiveFailedAttempts_LocksAccount()
    {
        var user = MakeUser(email: "lockout5@test.com");
        user.Password = BCrypt.Net.BCrypt.HashPassword("correct");
        user.FailedLoginAttempts = 4;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // 5th failed attempt triggers lockout
        var result = await _userService.LoginUserAsync(new LoginUserInputDto { Email = "lockout5@test.com", Password = "wrong" });

        result.Should().BeNull();
        var dbUser = await _context.Users.FirstAsync(u => u.Email == "lockout5@test.com");
        dbUser.LockoutUntil.Should().NotBeNull();
        dbUser.LockoutUntil!.Value.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), precision: TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task LoginUserAsync_LockedAccount_RejectsEvenWithCorrectPassword()
    {
        var user = MakeUser(email: "locked@test.com");
        user.Password = BCrypt.Net.BCrypt.HashPassword("correct");
        user.FailedLoginAttempts = 5;
        user.LockoutUntil = DateTime.UtcNow.AddMinutes(10);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result = await _userService.LoginUserAsync(new LoginUserInputDto { Email = "locked@test.com", Password = "correct" });

        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginUserAsync_ExpiredLockout_AllowsLogin()
    {
        var user = MakeUser(email: "expiredlock@test.com");
        user.Password = BCrypt.Net.BCrypt.HashPassword("correct");
        user.IsActive = true;
        user.FailedLoginAttempts = 5;
        user.LockoutUntil = DateTime.UtcNow.AddMinutes(-1); // lockout expired
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result = await _userService.LoginUserAsync(new LoginUserInputDto { Email = "expiredlock@test.com", Password = "correct" });

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task LoginUserAsync_SuccessAfterPreviousFails_ResetsCounter()
    {
        var user = MakeUser(email: "reset@test.com");
        user.Password = BCrypt.Net.BCrypt.HashPassword("correct");
        user.IsActive = true;
        user.FailedLoginAttempts = 3;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result = await _userService.LoginUserAsync(new LoginUserInputDto { Email = "reset@test.com", Password = "correct" });

        result.Should().NotBeNull();
        var dbUser = await _context.Users.FirstAsync(u => u.Email == "reset@test.com");
        dbUser.FailedLoginAttempts.Should().Be(0);
        dbUser.LockoutUntil.Should().BeNull();
    }

    // ─── Email Verification & Password Reset ─────────────────────────────────

    [Fact]
    public async Task VerifyEmailAsync_ValidToken_ActivatesUser()
    {
        var user = MakeUser(email: "verify@test.com", isActive: false);
        user.EmailVerificationToken = "valid-token-123";
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result = await _userService.VerifyEmailAsync("valid-token-123");

        result.Should().BeTrue();
        var dbUser = await _context.Users.FirstAsync(u => u.Email == "verify@test.com");
        dbUser.IsActive.Should().BeTrue();
        dbUser.EmailVerifiedAt.Should().NotBeNull();
        dbUser.EmailVerificationToken.Should().BeNull();
    }

    [Fact]
    public async Task VerifyEmailAsync_InvalidToken_ReturnsFalse()
    {
        var result = await _userService.VerifyEmailAsync("nonexistent-token");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidToken_UpdatesPassword()
    {
        var user = MakeUser(email: "resetpw@test.com");
        user.PasswordResetToken = "reset-token-abc";
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result = await _userService.ResetPasswordAsync("reset-token-abc", "newSecurePass");

        result.Should().BeTrue();
        var dbUser = await _context.Users.FirstAsync(u => u.Email == "resetpw@test.com");
        BCrypt.Net.BCrypt.Verify("newSecurePass", dbUser.Password).Should().BeTrue();
        dbUser.PasswordResetToken.Should().BeNull();
    }

    [Fact]
    public async Task ResetPasswordAsync_ExpiredToken_ReturnsFalse()
    {
        var user = MakeUser(email: "expiredtoken@test.com");
        user.PasswordResetToken = "expired-token";
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(-1); // expired
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result = await _userService.ResetPasswordAsync("expired-token", "newpass");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_UnknownEmail_DoesNotRevealExistence()
    {
        // Should complete without throwing — not reveal that email doesn't exist
        var act = async () => await _userService.SendPasswordResetEmailAsync("ghost@test.com");
        await act.Should().NotThrowAsync();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private void SetupUserContext(int userId, Guid userGuid)
    {
        var claims = new List<Claim> { new Claim("userId", userId.ToString()), new Claim("sub", userGuid.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
    }
}
