using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Lander;
using Lander.src.Modules.Users.Implementation.UserImplementation;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Modules.Users.Dtos.InputDto;
using Lander.Helpers;
using Lander.src.Modules.Communication.Intefaces;
using Lander.src.Modules.Listings.Interfaces;
using Lander.src.Modules.Roommates.Interfaces;

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
    private readonly UserService _userService;

    public UserServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<UsersContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var reviewsOptions = new DbContextOptionsBuilder<ReviewsContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new UsersContext(options);
        _reviewsContext = new ReviewsContext(reviewsOptions);

        // Setup mock configuration for TokenProvider
        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(x => x["Jwt:Secret"]).Returns("ThisIsAVerySecureSecretKeyForTestingPurposesOnly12345678");
        mockConfiguration.Setup(x => x["Jwt:Issuer"]).Returns("TestIssuer");
        mockConfiguration.Setup(x => x["Jwt:Audience"]).Returns("TestAudience");

        // Create real TokenProvider instance
        _tokenProvider = new TokenProvider(mockConfiguration.Object, _context);

        // Setup mocks
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockEmailService = new Mock<IEmailService>();
        _mockApartmentService = new Mock<IApartmentService>();
        _mockRoommateService = new Mock<IRoommateService>();

        // Create service instance
        _userService = new UserService(
            _context,
            _reviewsContext,
            _tokenProvider,
            _mockHttpContextAccessor.Object,
            _mockEmailService.Object,
            _mockApartmentService.Object,
            _mockRoommateService.Object
        );

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var tenantRole = new Role
        {
            RoleId = 1,
            RoleName = "Tenant",
            Description = "Tenant role for users looking for apartments",
            CreatedDate = DateTime.UtcNow
        };

        _context.Roles.Add(tenantRole);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _reviewsContext.Database.EnsureDeleted();
        _context.Dispose();
        _reviewsContext.Dispose();
    }

    #region RegisterUserAsync Tests

    [Fact]
    public async Task RegisterUserAsync_ValidInput_ShouldCreateUser()
    {
        // Arrange
        var registrationDto = new UserRegistrationInputDto
        {
            FirstName = "Marko",
            LastName = "Matovic",
            Email = "marko@test.com",
            Password = "Password123!",
            DateOfBirth = new DateTime(1990, 1, 1),
            PhoneNumber = "+381641234567",
            ProfilePicture = null
        };

        _mockEmailService
            .Setup(x => x.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _userService.RegisterUserAsync(registrationDto);

        // Assert
        result.Should().NotBeNull();
        result.FirstName.Should().Be("Marko");
        result.LastName.Should().Be("Matovic");
        result.Email.Should().Be("marko@test.com");
        result.IsActive.Should().BeTrue();
        result.UserRoleId.Should().Be(1); // Tenant role

        var userInDb = await _context.Users.FirstOrDefaultAsync(u => u.Email == "marko@test.com");
        userInDb.Should().NotBeNull();
        userInDb!.Password.Should().NotBe("Password123!"); // Password should be hashed
    }

    [Fact]
    public async Task RegisterUserAsync_ValidInput_ShouldSendWelcomeEmail()
    {
        // Arrange
        var registrationDto = new UserRegistrationInputDto
        {
            FirstName = "Ana",
            LastName = "Petrovic",
            Email = "ana@test.com",
            Password = "SecurePass456",
            DateOfBirth = new DateTime(1995, 5, 15),
            PhoneNumber = "+381651234567"
        };

        // Act
        await _userService.RegisterUserAsync(registrationDto);

        // Assert
        _mockEmailService.Verify(
            x => x.SendWelcomeEmailAsync("ana@test.com", "Ana Petrovic"),
            Times.Once
        );
    }

    [Fact]
    public async Task RegisterUserAsync_ShouldHashPassword()
    {
        // Arrange
        var registrationDto = new UserRegistrationInputDto
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@test.com",
            Password = "PlainTextPassword",
            DateOfBirth = new DateTime(1985, 3, 20),
            PhoneNumber = "+381661234567"
        };

        // Act
        await _userService.RegisterUserAsync(registrationDto);

        // Assert
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "test@test.com");
        user.Should().NotBeNull();
        user!.Password.Should().NotBe("PlainTextPassword");
        user.Password.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RegisterUserAsync_DuplicateEmail_ShouldThrowException()
    {
        // Arrange
        var input = new UserRegistrationInputDto
        {
            FirstName = "Marko",
            LastName = "Matovic",
            Email = "duplicate@test.com",
            Password = "Password123!"
        };
        await _userService.RegisterUserAsync(input);

        // Act
        var act = async () => await _userService.RegisterUserAsync(input);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("User with this email already exists.");
    }

    #endregion

    #region LoginUserAsync Tests

    [Fact]
    public async Task LoginUserAsync_ValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var password = "TestPassword123";
        var hashedPassword = HashPasswordForTest(password);

        var user = new User
        {
            FirstName = "Login",
            LastName = "Test",
            Email = "login@test.com",
            Password = hashedPassword,
            IsActive = true,
            UserRoleId = 1,
            UserGuid = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var loginDto = new LoginUserInputDto
        {
            Email = "login@test.com",
            Password = password
        };

        // Act
        var result = await _userService.LoginUserAsync(loginDto);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty(); // Real token will be generated
    }

    [Fact]
    public async Task LoginUserAsync_InvalidEmail_ShouldReturnNull()
    {
        // Arrange
        var loginDto = new LoginUserInputDto
        {
            Email = "nonexistent@test.com",
            Password = "SomePassword"
        };

        // Act
        var result = await _userService.LoginUserAsync(loginDto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginUserAsync_InvalidPassword_ShouldReturnNull()
    {
        // Arrange
        var correctPassword = "CorrectPassword";
        var hashedPassword = HashPasswordForTest(correctPassword);

        var user = new User
        {
            FirstName = "Wrong",
            LastName = "Password",
            Email = "wrongpass@test.com",
            Password = hashedPassword,
            IsActive = true,
            UserRoleId = 1,
            UserGuid = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var loginDto = new LoginUserInputDto
        {
            Email = "wrongpass@test.com",
            Password = "WrongPassword"
        };

        // Act
        var result = await _userService.LoginUserAsync(loginDto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginUserAsync_InactiveUser_ShouldReturnNull()
    {
        // Arrange
        var password = "TestPassword123";
        var hashedPassword = HashPasswordForTest(password);

        var user = new User
        {
            FirstName = "Inactive",
            LastName = "User",
            Email = "inactive_login@test.com",
            Password = hashedPassword,
            IsActive = false, // Deactivated
            UserRoleId = 1,
            UserGuid = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var loginDto = new LoginUserInputDto
        {
            Email = "inactive_login@test.com",
            Password = password
        };

        // Act
        var result = await _userService.LoginUserAsync(loginDto);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region DeactivateUserAsync Tests

    [Fact]
    public async Task DeactivateUserAsync_ExistingUser_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var userGuid = Guid.NewGuid();
        var user = new User
        {
            FirstName = "Active",
            LastName = "User",
            Email = "active@test.com",
            Password = "hashedpass",
            IsActive = true,
            UserRoleId = 1,
            UserGuid = userGuid,
            CreatedDate = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var deactivateDto = new DeactivateUserInputDto
        {
            UserGuid = userGuid
        };

        // Act
        await _userService.DeactivateUserAsync(deactivateDto);

        // Assert
        var deactivatedUser = await _context.Users.FirstOrDefaultAsync(u => u.UserGuid == userGuid);
        deactivatedUser.Should().NotBeNull();
        deactivatedUser!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateUserAsync_NonExistentUser_ShouldNotThrowException()
    {
        // Arrange
        var deactivateDto = new DeactivateUserInputDto
        {
            UserGuid = Guid.NewGuid()
        };

        // Act
        var act = async () => await _userService.DeactivateUserAsync(deactivateDto);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region ReactivateUserAsync Tests

    [Fact]
    public async Task ReactivateUserAsync_DeactivatedUser_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var userGuid = Guid.NewGuid();
        var user = new User
        {
            FirstName = "Inactive",
            LastName = "User",
            Email = "inactive@test.com",
            Password = "hashedpass",
            IsActive = false,
            UserRoleId = 1,
            UserGuid = userGuid,
            CreatedDate = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var reactivateDto = new ReactivateUserInputDto
        {
            UserGuid = userGuid
        };

        // Act
        await _userService.ReactivateUserAsync(reactivateDto);

        // Assert
        var reactivatedUser = await _context.Users.FirstOrDefaultAsync(u => u.UserGuid == userGuid);
        reactivatedUser.Should().NotBeNull();
        reactivatedUser!.IsActive.Should().BeTrue();
    }

    #endregion

    #region GetUserByGuidAsync Tests

    [Fact]
    public async Task GetUserByGuidAsync_ExistingUser_ShouldReturnUser()
    {
        // Arrange
        var userGuid = Guid.NewGuid();
        var user = new User
        {
            FirstName = "Find",
            LastName = "Me",
            Email = "findme@test.com",
            Password = "hashedpass",
            IsActive = true,
            UserRoleId = 1,
            UserGuid = userGuid,
            CreatedDate = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetUserByGuidAsync(userGuid);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("findme@test.com");
        result.FirstName.Should().Be("Find");
    }

    [Fact]
    public async Task GetUserByGuidAsync_NonExistentUser_ShouldReturnNull()
    {
        // Arrange
        var nonExistentGuid = Guid.NewGuid();

        // Act
        var result = await _userService.GetUserByGuidAsync(nonExistentGuid);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    private string HashPasswordForTest(string password)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }

    private void SetupUserContext(int userId, Guid userGuid)
    {
        var claims = new List<Claim>
        {
            new Claim("userId", userId.ToString()),
            new Claim("sub", userGuid.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userGuid.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
    }
    
    #region ChangePassword Tests

    [Fact]
    public async Task ChangePasswordAsync_ValidOldPassword_ShouldUpdatePassword()
    {
        // Arrange
        var userGuid = Guid.NewGuid();
        var oldPassword = "OldPassword123";
        var newPassword = "NewPassword123";
        
        var user = new User
        {
            FirstName = "Change",
            LastName = "Pass",
            Email = "change@test.com",
            Password = HashPasswordForTest(oldPassword),
            IsActive = true,
            UserRoleId = 1,
            UserGuid = userGuid,
            CreatedDate = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        SetupUserContext(user.UserId, userGuid);

        var changeDto = new ChangePasswordInputDto
        {
            OldPassword = oldPassword,
            NewPassword = newPassword
        };

        // Act
        await _userService.ChangePasswordAsync(changeDto);

        // Assert
        var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.UserGuid == userGuid);
        updatedUser!.Password.Should().Be(HashPasswordForTest(newPassword));
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongOldPassword_ShouldThrowException()
    {
        // Arrange
        var userGuid = Guid.NewGuid();
        var user = new User
        {
            FirstName = "Wrong",
            LastName = "Old",
            Email = "wrongold@test.com",
            Password = HashPasswordForTest("CorrectOld"),
            IsActive = true,
            UserRoleId = 1,
            UserGuid = userGuid,
            CreatedDate = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        SetupUserContext(user.UserId, userGuid);

        var changeDto = new ChangePasswordInputDto
        {
            OldPassword = "WrongOld",
            NewPassword = "NewPassword"
        };

        // Act
        var act = async () => await _userService.ChangePasswordAsync(changeDto);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Incorrect old password.");
    }

    #endregion

    #endregion
}
