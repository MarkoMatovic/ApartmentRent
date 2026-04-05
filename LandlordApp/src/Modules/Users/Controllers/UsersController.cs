using System.Security.Claims;
using Lander.Helpers;
using Lander.src.Modules.Users.Dtos.Dto;
using Lander.src.Modules.Users.Dtos.InputDto;
using Lander.src.Modules.Users.Implementation.UserImplementation;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
namespace Lander.src.Modules.Users.Controllers
{
    [Route(ApiActionsV1.Auth)]
    [ApiController]
    public class UsersController : ControllerBase
    {
        #region Properties
        private readonly IUserInterface _userInterface;
        private readonly TokenProvider _tokenProvider;
        private readonly UsersContext _usersContext;
        private readonly RefreshTokenService _refreshTokenService;
        #endregion
        #region Constructors
        public UsersController(IUserInterface userInterface, TokenProvider tokenProvider, UsersContext usersContext, RefreshTokenService refreshTokenService)
        {
            _userInterface = userInterface;
            _tokenProvider = tokenProvider;
            _usersContext = usersContext;
            _refreshTokenService = refreshTokenService;
        }
        #endregion
        [EnableRateLimiting("auth")]
        [HttpPost(ApiActionsV1.Register, Name = nameof(ApiActionsV1.Register))]
        public async Task<ActionResult<UserRegistrationDto>> RegisterUser([FromBody] UserRegistrationInputDto userRegistrationInputDto)
        {
            return Ok(await _userInterface.RegisterUserAsync(userRegistrationInputDto));
        }

        [EnableRateLimiting("auth")]
        [HttpPost(ApiActionsV1.Login, Name = nameof(ApiActionsV1.Login))]
        public async Task<ActionResult<AuthTokenDto>> LoginUser([FromBody] LoginUserInputDto loginUserInputDto)
        {
            var tokens = await _userInterface.LoginUserAsync(loginUserInputDto);
            if (tokens == null)
                return Unauthorized(new { message = "Neispravan email ili lozinka" });

            // Store refresh token in httpOnly cookie — not accessible to JS (XSS protection)
            SetRefreshTokenCookie(tokens.RefreshToken);

            // Return only the access token in the body; refresh token travels via cookie
            return Ok(new AuthTokenDto { AccessToken = tokens.AccessToken, RefreshToken = string.Empty });
        }
        [HttpPost(ApiActionsV1.Logout, Name = nameof(ApiActionsV1.Logout))]
        public async Task<ActionResult> Logout([FromBody] LogoutInputDto? dto = null)
        {
            // Accept refresh token from cookie (preferred) or body (fallback for old clients)
            var rawRefreshToken = Request.Cookies["refreshToken"] ?? dto?.RefreshToken;
            await _userInterface.LogoutUserAsync(rawRefreshToken);

            // Clear the httpOnly cookie
            Response.Cookies.Delete("refreshToken", new CookieOptions
            {
                Path = "/api/v1/auth",
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            return Ok(new { Message = "Logged out successfully" });
        }
        [EnableRateLimiting("auth")]
        [HttpPost(ApiActionsV1.ChangePassword, Name = nameof(ApiActionsV1.ChangePassword))]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordInputDto changePasswordInputDto)
        {
            await _userInterface.ChangePasswordAsync(changePasswordInputDto);
            return Ok();
        }
        [HttpDelete(ApiActionsV1.DeleteUser, Name = nameof(ApiActionsV1.DeleteUser))]
        public async Task<ActionResult<bool>> DeleteUser(DeleteUserInputDto deleteUserInputDto)
        {
            await _userInterface.DeleteUserAsync(deleteUserInputDto);
            return Ok(true);
        }
        [HttpPost(ApiActionsV1.DeactivateUser, Name = nameof(ApiActionsV1.DeactivateUser))]
        public async Task<IActionResult> DeactivateUser([FromBody] DeactivateUserInputDto deactivateUserInputDto)
        {
            await _userInterface.DeactivateUserAsync(deactivateUserInputDto);
            return Ok("User deactivated successfully.");
        }
        [HttpPost(ApiActionsV1.ReactivateUser, Name = nameof(ApiActionsV1.ReactivateUser))]
        public async Task<IActionResult> ReactivateUser([FromBody] ReactivateUserInputDto reactivateUserInputDto)
        {
            await _userInterface.ReactivateUserAsync(reactivateUserInputDto);
            return Ok("User reactivated successfully.");
        }
        [HttpPost(ApiActionsV1.UpdateRoommateStatus, Name = nameof(ApiActionsV1.UpdateRoommateStatus))]
        public async Task<IActionResult> UpdateRoommateStatus([FromBody] UpdateRoommateStatusInputDto updateRoommateStatusInputDto)
        {
            await _userInterface.UpdateRoommateStatusAsync(updateRoommateStatusInputDto);
            return Ok("Roommate status updated successfully.");
        }
        [HttpGet(ApiActionsV1.GetUserProfile, Name = nameof(ApiActionsV1.GetUserProfile))]
        public async Task<ActionResult<UserProfileDto>> GetUserProfile([FromRoute] int userId)
        {
            var profile = await _userInterface.GetUserProfileAsync(userId);
            if (profile == null)
                return NotFound("User not found");
            return Ok(profile);
        }
        [HttpPut(ApiActionsV1.UpdateUserProfile, Name = nameof(ApiActionsV1.UpdateUserProfile))]
        public async Task<ActionResult<UserProfileDto>> UpdateUserProfile([FromRoute] int userId, [FromBody] UserProfileUpdateInputDto updateDto)
        {
            var profile = await _userInterface.UpdateUserProfileAsync(userId, updateDto);
            return Ok(profile);
        }
        [HttpPut("update-privacy-settings/{userId}")]
        public async Task<ActionResult<UserProfileDto>> UpdatePrivacySettings([FromRoute] int userId, [FromBody] PrivacySettingsDto privacySettingsDto)
        {
            var profile = await _userInterface.UpdatePrivacySettingsAsync(userId, privacySettingsDto);
            return Ok(profile);
        }
        [HttpGet("export-data/{userId}")]
        public async Task<ActionResult<UserExportDto>> ExportUserData([FromRoute] int userId)
        {
            var exportData = await _userInterface.ExportUserDataAsync(userId);
            return Ok(exportData);
        }

        /// <summary>
        /// Rotates refresh token: validates old refresh token, issues new access + refresh token pair.
        /// Accepts refresh token from httpOnly cookie (preferred) or JSON body (fallback).
        /// </summary>
        [HttpPost("token/refresh")]
        public async Task<ActionResult<AuthTokenDto>> RotateRefreshToken([FromBody] LogoutInputDto? dto = null)
        {
            // Cookie takes priority; body accepted as backward-compat fallback
            var rawRefreshToken = Request.Cookies["refreshToken"] ?? dto?.RefreshToken;

            if (string.IsNullOrEmpty(rawRefreshToken))
                return BadRequest(new { message = "Refresh token je obavezan." });

            var storedToken = await _refreshTokenService.ValidateAsync(rawRefreshToken);
            if (storedToken is null)
                return Unauthorized(new { message = "Refresh token je nevažeći ili je istekao." });

            await _refreshTokenService.RevokeAsync(rawRefreshToken);

            var user = storedToken.User;
            var newAccess = await _tokenProvider.CreateAsync(user);
            var newRefresh = await _refreshTokenService.CreateAsync(user.UserId);

            // Rotate cookie
            SetRefreshTokenCookie(newRefresh);

            return Ok(new AuthTokenDto { AccessToken = newAccess, RefreshToken = string.Empty });
        }

        /// <summary>
        /// Re-issues a fresh JWT token with updated claims (e.g. after subscription purchase).
        /// Requires a valid Bearer access token.
        /// </summary>
        [HttpPost("refresh-token")]
        [Authorize]
        public async Task<ActionResult<string>> RefreshToken()
        {
            var userGuid = User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(userGuid)) return Unauthorized();

            var user = await _usersContext.Users
                .Include(u => u.UserRole)
                .FirstOrDefaultAsync(u => u.UserGuid == Guid.Parse(userGuid));

            if (user == null) return Unauthorized();

            var token = await _tokenProvider.CreateAsync(user);
            return Ok(token);
        }

        [HttpPost("send-verification-email/{userId}")]
        [Authorize]
        public async Task<IActionResult> SendVerificationEmail([FromRoute] int userId)
        {
            var currentId = int.Parse(User.FindFirstValue("userId") ?? "0");
            if (currentId != userId) return Forbid();
            await _userInterface.SendVerificationEmailAsync(userId);
            return Ok(new { message = "Email verifikacije je poslan." });
        }

        [HttpGet("verify-email")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token)) return BadRequest("Token je obavezan.");
            var success = await _userInterface.VerifyEmailAsync(token);
            if (!success) return BadRequest(new { message = "Token je nevažeći ili je istekao." });
            return Ok(new { message = "Email je uspješno verifikovan." });
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordInputDto dto)
        {
            await _userInterface.SendPasswordResetEmailAsync(dto.Email);
            // Always return OK to avoid email enumeration
            return Ok(new { message = "Ako email postoji, poslan je link za reset lozinke." });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordInputDto dto)
        {
            var success = await _userInterface.ResetPasswordAsync(dto.Token, dto.NewPassword);
            if (!success) return BadRequest(new { message = "Token je nevažeći ili je istekao." });
            return Ok(new { message = "Lozinka je uspješno promijenjena." });
        }

        // ─── helpers ────────────────────────────────────────────────────────────
        private void SetRefreshTokenCookie(string rawRefreshToken)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly  = true,     // Not accessible to JavaScript (XSS protection)
                Secure    = true,     // Sent only over HTTPS
                SameSite  = SameSiteMode.Strict, // CSRF protection
                Expires   = DateTimeOffset.UtcNow.AddDays(30),
                Path      = "/api/v1/auth" // Scope to auth endpoints only
            };
            Response.Cookies.Append("refreshToken", rawRefreshToken, cookieOptions);
        }
    }
}
