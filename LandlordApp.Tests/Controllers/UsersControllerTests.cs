using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Lander;
using Lander.Helpers;
using Lander.src.Modules.Users.Controllers;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Modules.Users.Dtos.Dto;
using Lander.src.Modules.Users.Dtos.InputDto;
using Lander.src.Modules.Users.Implementation.UserImplementation;
using Lander.src.Modules.Users.Interfaces.UserInterface;

namespace LandlordApp.Tests.Controllers;

public class UsersControllerTests : IDisposable
{
    private readonly Mock<IUserInterface> _mockUserService;
    private readonly UsersContext _usersContext;
    private readonly TokenProvider _tokenProvider;
    private readonly RefreshTokenService _refreshTokenService;
    private readonly UsersController _controller;

    private static readonly Guid TestGuid = Guid.NewGuid();
    private const int TestUserId = 42;

    public UsersControllerTests()
    {
        _mockUserService = new Mock<IUserInterface>();

        var opts = new DbContextOptionsBuilder<UsersContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _usersContext = new UsersContext(opts);

        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "ThisIsAVerySecureSecretKeyForTestingPurposesOnly12345678",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience"
            })
            .Build();

        _tokenProvider = new TokenProvider(cfg, _usersContext);
        _refreshTokenService = new RefreshTokenService(_usersContext);

        _controller = new UsersController(_mockUserService.Object, _tokenProvider, _usersContext, _refreshTokenService);
        _controller.ControllerContext = MakeAuthContext(TestUserId, TestGuid);
    }

    public void Dispose()
    {
        _usersContext.Database.EnsureDeleted();
        _usersContext.Dispose();
    }

    // ─── Register ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterUser_ValidInput_ReturnsOk()
    {
        var dto = new UserRegistrationInputDto { FirstName = "A", LastName = "B", Email = "a@b.com", Password = "pass" };
        var expected = new UserRegistrationDto { Email = "a@b.com" };
        _mockUserService.Setup(s => s.RegisterUserAsync(dto)).ReturnsAsync(expected);

        var result = await _controller.RegisterUser(dto);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(expected);
    }

    // ─── Login ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginUser_ValidCredentials_ReturnsOkWithAccessToken()
    {
        var dto = new LoginUserInputDto { Email = "a@b.com", Password = "pass" };
        _mockUserService.Setup(s => s.LoginUserAsync(dto))
            .ReturnsAsync(new AuthTokenDto { AccessToken = "access123", RefreshToken = "refresh123" });

        // Need response cookies support
        _controller.ControllerContext.HttpContext.Response.Headers.Clear();
        var result = await _controller.LoginUser(dto);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var token = ok.Value.Should().BeOfType<AuthTokenDto>().Subject;
        token.AccessToken.Should().Be("access123");
        // RefreshToken is stripped from body — set via httpOnly cookie
        token.RefreshToken.Should().BeEmpty();
    }

    [Fact]
    public async Task LoginUser_InvalidCredentials_ReturnsUnauthorized()
    {
        var dto = new LoginUserInputDto { Email = "a@b.com", Password = "wrong" };
        _mockUserService.Setup(s => s.LoginUserAsync(dto)).ReturnsAsync((AuthTokenDto?)null);

        var result = await _controller.LoginUser(dto);

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    // ─── Logout ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_ReturnsOk()
    {
        _mockUserService.Setup(s => s.LogoutUserAsync(It.IsAny<string?>())).Returns(Task.CompletedTask);

        var result = await _controller.Logout(null);

        result.Should().BeOfType<OkObjectResult>();
    }

    // ─── Change Password ──────────────────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_ReturnsOk()
    {
        var dto = new ChangePasswordInputDto { OldPassword = "old", NewPassword = "new" };
        _mockUserService.Setup(s => s.ChangePasswordAsync(dto)).Returns(Task.CompletedTask);

        var result = await _controller.ChangePassword(dto);

        result.Should().BeOfType<OkResult>();
    }

    // ─── Delete ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteUser_ReturnsOkTrue()
    {
        var dto = new DeleteUserInputDto { UserGuid = TestGuid };
        _mockUserService.Setup(s => s.DeleteUserAsync(dto)).ReturnsAsync(true);

        var result = await _controller.DeleteUser(dto);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(true);
    }

    // ─── Deactivate / Reactivate ──────────────────────────────────────────────

    [Fact]
    public async Task DeactivateUser_ReturnsOk()
    {
        var dto = new DeactivateUserInputDto { UserGuid = TestGuid };
        _mockUserService.Setup(s => s.DeactivateUserAsync(dto)).Returns(Task.CompletedTask);

        var result = await _controller.DeactivateUser(dto);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ReactivateUser_ReturnsOk()
    {
        var dto = new ReactivateUserInputDto { UserGuid = TestGuid };
        _mockUserService.Setup(s => s.ReactivateUserAsync(dto)).Returns(Task.CompletedTask);

        var result = await _controller.ReactivateUser(dto);

        result.Should().BeOfType<OkObjectResult>();
    }

    // ─── Profile ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserProfile_Found_ReturnsOk()
    {
        var profile = new UserProfileDto { UserId = TestUserId, Email = "a@b.com" };
        _mockUserService.Setup(s => s.GetUserProfileAsync(TestUserId)).ReturnsAsync(profile);

        var result = await _controller.GetUserProfile(TestUserId);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(profile);
    }

    [Fact]
    public async Task GetUserProfile_NotFound_ReturnsNotFound()
    {
        _mockUserService.Setup(s => s.GetUserProfileAsync(99)).ReturnsAsync((UserProfileDto?)null);

        var result = await _controller.GetUserProfile(99);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateUserProfile_ReturnsOk()
    {
        var dto = new UserProfileUpdateInputDto { FirstName = "New" };
        var updated = new UserProfileDto { FirstName = "New" };
        _mockUserService.Setup(s => s.UpdateUserProfileAsync(TestUserId, dto)).ReturnsAsync(updated);

        var result = await _controller.UpdateUserProfile(TestUserId, dto);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(updated);
    }

    [Fact]
    public async Task UpdatePrivacySettings_ReturnsOk()
    {
        var dto = new PrivacySettingsDto { AnalyticsConsent = true };
        _mockUserService.Setup(s => s.UpdatePrivacySettingsAsync(TestUserId, dto))
            .ReturnsAsync(new UserProfileDto());

        var result = await _controller.UpdatePrivacySettings(TestUserId, dto);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ExportUserData_ReturnsOk()
    {
        _mockUserService.Setup(s => s.ExportUserDataAsync(TestUserId))
            .ReturnsAsync(new UserExportDto { UserProfile = new UserProfileDto() });

        var result = await _controller.ExportUserData(TestUserId);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    // ─── Refresh Token Rotation ───────────────────────────────────────────────

    [Fact]
    public async Task RotateRefreshToken_NoToken_ReturnsBadRequest()
    {
        // No cookie, no body
        var result = await _controller.RotateRefreshToken(null);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RotateRefreshToken_InvalidToken_ReturnsUnauthorized()
    {
        var dto = new LogoutInputDto { RefreshToken = "bad-token" };

        var result = await _controller.RotateRefreshToken(dto);

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task RotateRefreshToken_ValidToken_ReturnsNewAccessToken()
    {
        // Seed user + role + valid refresh token
        _usersContext.Roles.Add(new Role { RoleId = 1, RoleName = "Tenant", Description = "t", CreatedDate = DateTime.UtcNow });
        _usersContext.Users.Add(new User
        {
            UserId = TestUserId, FirstName = "T", LastName = "U",
            Email = "t@u.com", Password = "hash", IsActive = true,
            UserRoleId = 1, UserGuid = TestGuid
        });
        await _usersContext.SaveChangesAsync();

        var raw = await _refreshTokenService.CreateAsync(TestUserId);
        var dto = new LogoutInputDto { RefreshToken = raw };

        var result = await _controller.RotateRefreshToken(dto);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<AuthTokenDto>()
            .Which.AccessToken.Should().NotBeNullOrEmpty();
    }

    // ─── Email Verification ───────────────────────────────────────────────────

    [Fact]
    public async Task VerifyEmail_EmptyToken_ReturnsBadRequest()
    {
        var result = await _controller.VerifyEmail(string.Empty);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task VerifyEmail_InvalidToken_ReturnsBadRequest()
    {
        _mockUserService.Setup(s => s.VerifyEmailAsync("bad")).ReturnsAsync(false);

        var result = await _controller.VerifyEmail("bad");
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task VerifyEmail_ValidToken_ReturnsOk()
    {
        _mockUserService.Setup(s => s.VerifyEmailAsync("good")).ReturnsAsync(true);

        var result = await _controller.VerifyEmail("good");
        result.Should().BeOfType<OkObjectResult>();
    }

    // ─── Forgot / Reset Password ──────────────────────────────────────────────

    [Fact]
    public async Task ForgotPassword_AlwaysReturnsOk()
    {
        _mockUserService.Setup(s => s.SendPasswordResetEmailAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        var result = await _controller.ForgotPassword(new ForgotPasswordInputDto { Email = "any@test.com" });
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ResetPassword_ValidToken_ReturnsOk()
    {
        _mockUserService.Setup(s => s.ResetPasswordAsync("tok", "newpass")).ReturnsAsync(true);

        var result = await _controller.ResetPassword(new ResetPasswordInputDto { Token = "tok", NewPassword = "newpass" });
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_ReturnsBadRequest()
    {
        _mockUserService.Setup(s => s.ResetPasswordAsync("bad", "newpass")).ReturnsAsync(false);

        var result = await _controller.ResetPassword(new ResetPasswordInputDto { Token = "bad", NewPassword = "newpass" });
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ─── RefreshToken (POST refresh-token, requires valid Bearer) ────────────

    [Fact]
    public async Task RefreshToken_MissingSubClaim_ReturnsUnauthorized()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        var result = await _controller.RefreshToken();

        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task RefreshToken_UserNotFoundInDb_ReturnsUnauthorized()
    {
        // Sub GUID that has no matching user in the in-memory DB
        var unknownGuid = Guid.NewGuid();
        _controller.ControllerContext = MakeAuthContext(99, unknownGuid);

        var result = await _controller.RefreshToken();

        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task RefreshToken_ValidUser_ReturnsNewAccessToken()
    {
        _usersContext.Roles.Add(new Role { RoleId = 1, RoleName = "Tenant", Description = "t", CreatedDate = DateTime.UtcNow });
        _usersContext.Users.Add(new User
        {
            UserId = TestUserId, FirstName = "T", LastName = "U",
            Email = "t@u.com", Password = "hash", IsActive = true,
            UserRoleId = 1, UserGuid = TestGuid
        });
        await _usersContext.SaveChangesAsync();

        // TestGuid is already set as the "sub" claim in the default controller context
        var result = await _controller.RefreshToken();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<string>().Which.Should().NotBeNullOrEmpty();
    }

    // ─── SendVerificationEmail ────────────────────────────────────────────────

    [Fact]
    public async Task SendVerificationEmail_SameUser_ReturnsOk()
    {
        _mockUserService.Setup(s => s.SendVerificationEmailAsync(TestUserId)).Returns(Task.CompletedTask);

        var result = await _controller.SendVerificationEmail(TestUserId);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SendVerificationEmail_DifferentUser_ReturnsForbid()
    {
        // Current user is TestUserId (42), trying to send email for user 99
        var result = await _controller.SendVerificationEmail(99);

        result.Should().BeOfType<ForbidResult>();
        _mockUserService.Verify(s => s.SendVerificationEmailAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task SendVerificationEmail_ServiceThrows_PropagatesException()
    {
        _mockUserService.Setup(s => s.SendVerificationEmailAsync(TestUserId))
            .ThrowsAsync(new Exception("Email error"));

        Func<Task> act = async () => await _controller.SendVerificationEmail(TestUserId);

        await act.Should().ThrowAsync<Exception>().WithMessage("Email error");
    }

    // ─── UpdateRoommateStatus ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateRoommateStatus_ReturnsOk()
    {
        var dto = new UpdateRoommateStatusInputDto { UserGuid = TestGuid, IsLookingForRoommate = true };
        _mockUserService.Setup(s => s.UpdateRoommateStatusAsync(dto)).Returns(Task.CompletedTask);

        var result = await _controller.UpdateRoommateStatus(dto);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateRoommateStatus_ServiceThrows_PropagatesException()
    {
        var dto = new UpdateRoommateStatusInputDto { UserGuid = TestGuid, IsLookingForRoommate = false };
        _mockUserService.Setup(s => s.UpdateRoommateStatusAsync(dto))
            .ThrowsAsync(new Exception("Status error"));

        Func<Task> act = async () => await _controller.UpdateRoommateStatus(dto);

        await act.Should().ThrowAsync<Exception>().WithMessage("Status error");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static ControllerContext MakeAuthContext(int userId, Guid userGuid)
    {
        var claims = new List<Claim>
        {
            new("userId", userId.ToString()),
            new("sub", userGuid.ToString())
        };
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
        };
        return new ControllerContext { HttpContext = httpContext };
    }
}
