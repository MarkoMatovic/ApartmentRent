using System.Security.Claims;
using Lander.Helpers;
using Lander.src.Common.Exceptions;
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
        private readonly RefreshTokenService _refreshTokenService;
        #endregion
        #region Constructors
        public UsersController(IUserInterface userInterface, TokenProvider tokenProvider, RefreshTokenService refreshTokenService)
        {
            _userInterface = userInterface;
            _tokenProvider = tokenProvider;
            _refreshTokenService = refreshTokenService;
        }
        #endregion
        [EnableRateLimiting("register")]
        [HttpPost(ApiActionsV1.Register, Name = nameof(ApiActionsV1.Register))]
        public async Task<ActionResult<UserRegistrationDto>> RegisterUser([FromBody] UserRegistrationInputDto userRegistrationInputDto)
        {
            return Ok(await _userInterface.RegisterUserAsync(userRegistrationInputDto));
        }

        [EnableRateLimiting("login")]
        [HttpPost(ApiActionsV1.Login, Name = nameof(ApiActionsV1.Login))]
        public async Task<ActionResult<AuthTokenDto>> LoginUser([FromBody] LoginUserInputDto loginUserInputDto)
        {
            AuthTokenDto? tokens;
            try
            {
                tokens = await _userInterface.LoginUserAsync(loginUserInputDto);
            }
            catch (ForbiddenException ex) when (ex.Message == "EMAIL_NOT_VERIFIED")
            {
                return StatusCode(403, new { message = "EMAIL_NOT_VERIFIED" });
            }
            if (tokens == null)
                return Unauthorized(new { message = "Neispravan email ili lozinka" });

            SetRefreshTokenCookie(tokens.RefreshToken);

            return Ok(new AuthTokenDto { AccessToken = tokens.AccessToken, RefreshToken = string.Empty });
        }
        [HttpPost(ApiActionsV1.Logout, Name = nameof(ApiActionsV1.Logout))]
        public async Task<ActionResult> Logout([FromBody] LogoutInputDto? dto = null)
        {
            
            var rawRefreshToken = Request.Cookies["refreshToken"] ?? dto?.RefreshToken;
            await _userInterface.LogoutUserAsync(rawRefreshToken);

         
            Response.Cookies.Delete("refreshToken", new CookieOptions
            {
                Path = "/api/v1/auth",
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            return Ok(new { Message = "Logged out successfully" });
        }
        [EnableRateLimiting("change-password")]
        [Authorize]
        [HttpPost(ApiActionsV1.ChangePassword, Name = nameof(ApiActionsV1.ChangePassword))]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordInputDto changePasswordInputDto)
        {
            await _userInterface.ChangePasswordAsync(changePasswordInputDto);
            return Ok();
        }
        [HttpDelete(ApiActionsV1.DeleteUser, Name = nameof(ApiActionsV1.DeleteUser))]
        [Authorize]
        public async Task<ActionResult<bool>> DeleteUser([FromRoute] Guid userGuid)
        {
            if (!Guid.TryParse(User.FindFirstValue("sub"), out var callerGuid) ||
                (callerGuid != userGuid && !User.IsInRole("Admin"))) return Forbid();
            await _userInterface.DeleteUserAsync(new DeleteUserInputDto { UserGuid = userGuid });
            return Ok(true);
        }
        [HttpPost(ApiActionsV1.DeactivateUser, Name = nameof(ApiActionsV1.DeactivateUser))]
        [Authorize]
        public async Task<IActionResult> DeactivateUser([FromBody] DeactivateUserInputDto deactivateUserInputDto)
        {
            if (!Guid.TryParse(User.FindFirstValue("sub"), out var callerGuid) ||
                (callerGuid != deactivateUserInputDto.UserGuid && !User.IsInRole("Admin"))) return Forbid();
            await _userInterface.DeactivateUserAsync(deactivateUserInputDto);
            return Ok("User deactivated successfully.");
        }
        [HttpPost(ApiActionsV1.ReactivateUser, Name = nameof(ApiActionsV1.ReactivateUser))]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReactivateUser([FromBody] ReactivateUserInputDto reactivateUserInputDto)
        {
            await _userInterface.ReactivateUserAsync(reactivateUserInputDto);
            return Ok("User reactivated successfully.");
        }
        [HttpPost(ApiActionsV1.UpdateRoommateStatus, Name = nameof(ApiActionsV1.UpdateRoommateStatus))]
        [Authorize]
        public async Task<IActionResult> UpdateRoommateStatus([FromBody] UpdateRoommateStatusInputDto updateRoommateStatusInputDto)
        {
            if (!Guid.TryParse(User.FindFirstValue("sub"), out var callerGuid) ||
                (callerGuid != updateRoommateStatusInputDto.UserGuid && !User.IsInRole("Admin"))) return Forbid();
            await _userInterface.UpdateRoommateStatusAsync(updateRoommateStatusInputDto);
            return Ok("Roommate status updated successfully.");
        }
        [HttpGet(ApiActionsV1.GetUserProfile, Name = nameof(ApiActionsV1.GetUserProfile))]
        [Authorize]
        public async Task<ActionResult<UserProfileDto>> GetUserProfile([FromRoute] int userId)
        {
            var profile = await _userInterface.GetUserProfileAsync(userId);
            if (profile == null)
                return NotFound("User not found");

            // Own profile or Admin → full data
            if (!int.TryParse(User.FindFirstValue("userId"), out var callerId) || callerId != userId)
            {
                if (!User.IsInRole("Admin"))
                {
                    // Sanitize PII from other users' profiles
                    profile.Email = string.Empty;
                    profile.PhoneNumber = null;
                    profile.DateOfBirth = null;
                    profile.UserGuid = Guid.Empty;
                    profile.TokenBalance = 0;
                    profile.AnalyticsConsent = false;
                    profile.ChatHistoryConsent = false;
                    profile.UserRoleId = null;
                    profile.RoleName = null;
                    profile.CreatedDate = null;
                }
            }

            return Ok(profile);
        }
        [HttpPut(ApiActionsV1.UpdateUserProfile, Name = nameof(ApiActionsV1.UpdateUserProfile))]
        [Authorize]
        public async Task<ActionResult<UserProfileDto>> UpdateUserProfile([FromRoute] int userId, [FromBody] UserProfileUpdateInputDto updateDto)
        {
            if (!int.TryParse(User.FindFirstValue("userId"), out var callerId) || (callerId != userId && !User.IsInRole("Admin")))
                return Forbid();
            var profile = await _userInterface.UpdateUserProfileAsync(userId, updateDto);
            return Ok(profile);
        }
        [HttpPut(ApiActionsV1.UpdatePrivacySettings, Name = nameof(ApiActionsV1.UpdatePrivacySettings))]
        [Authorize]
        public async Task<ActionResult<UserProfileDto>> UpdatePrivacySettings([FromRoute] int userId, [FromBody] PrivacySettingsDto privacySettingsDto)
        {
            if (!int.TryParse(User.FindFirstValue("userId"), out var callerId) || (callerId != userId && !User.IsInRole("Admin")))
                return Forbid();
            var profile = await _userInterface.UpdatePrivacySettingsAsync(userId, privacySettingsDto);
            return Ok(profile);
        }
        [HttpGet(ApiActionsV1.ExportUserData, Name = nameof(ApiActionsV1.ExportUserData))]
        [Authorize]
        public async Task<ActionResult<UserExportDto>> ExportUserData([FromRoute] int userId)
        {
            if (!int.TryParse(User.FindFirstValue("userId"), out var callerId) || (callerId != userId && !User.IsInRole("Admin")))
                return Forbid();
            var exportData = await _userInterface.ExportUserDataAsync(userId);
            return Ok(exportData);
        }

        
        [HttpPost(ApiActionsV1.RefreshToken, Name = nameof(ApiActionsV1.RefreshToken))]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<AuthTokenDto>> RotateRefreshToken([FromBody] LogoutInputDto? dto = null)
        {
            
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

            SetRefreshTokenCookie(newRefresh);

            return Ok(new AuthTokenDto { AccessToken = newAccess, RefreshToken = string.Empty });
        }

        [HttpPost(ApiActionsV1.SendVerificationEmail, Name = nameof(ApiActionsV1.SendVerificationEmail))]
        [Authorize]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> SendVerificationEmail([FromRoute] int userId)
        {
            var currentId = int.Parse(User.FindFirstValue("userId") ?? "0");
            if (currentId != userId) return Forbid();
            await _userInterface.SendVerificationEmailAsync(userId);
            return Ok(new { message = "Email verifikacije je poslan." });
        }

        [HttpGet(ApiActionsV1.VerifyEmail, Name = nameof(ApiActionsV1.VerifyEmail))]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token)) return BadRequest("Token je obavezan.");
            var success = await _userInterface.VerifyEmailAsync(token);
            if (!success) return BadRequest(new { message = "Token je nevažeći ili je istekao." });
            return Ok(new { message = "Email je uspješno verifikovan." });
        }

        [HttpPost(ApiActionsV1.ForgotPassword, Name = nameof(ApiActionsV1.ForgotPassword))]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordInputDto dto)
        {
            await _userInterface.SendPasswordResetEmailAsync(dto.Email);
            return Ok(new { message = "Ako email postoji, poslan je link za reset lozinke." });
        }

        [HttpPost(ApiActionsV1.ResetPassword, Name = nameof(ApiActionsV1.ResetPassword))]
        [AllowAnonymous]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordInputDto dto)
        {
            var success = await _userInterface.ResetPasswordAsync(dto.Token, dto.NewPassword);
            if (!success) return BadRequest(new { message = "Token je nevažeći ili je istekao." });
            return Ok(new { message = "Lozinka je uspješno promijenjena." });
        }

        private void SetRefreshTokenCookie(string rawRefreshToken)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly  = true,     
                Secure    = true,  
                SameSite  = SameSiteMode.Strict,
                Expires   = DateTimeOffset.UtcNow.AddDays(30),
                Path      = "/api/v1/auth" 
            };
            Response.Cookies.Append("refreshToken", rawRefreshToken, cookieOptions);
        }
    }
}
