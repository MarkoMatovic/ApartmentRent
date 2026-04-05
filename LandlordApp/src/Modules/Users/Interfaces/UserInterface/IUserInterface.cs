using Lander.src.Modules.Users.Dtos.Dto;
using Lander.src.Modules.Users.Dtos.InputDto;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
namespace Lander.src.Modules.Users.Interfaces.UserInterface;
public interface IUserInterface
{
    Task<UserRegistrationDto> RegisterUserAsync(UserRegistrationInputDto userRegistrationInputDto);
    Task<AuthTokenDto?> LoginUserAsync(LoginUserInputDto userRegistrationInputDto);
    Task LogoutUserAsync(string? rawRefreshToken = null);
    Task DeactivateUserAsync(DeactivateUserInputDto deactivateUserInputDto);
    Task ReactivateUserAsync(ReactivateUserInputDto reactivateUserInputDto);
    Task<bool> DeleteUserAsync(DeleteUserInputDto deleteUserInputDto);
    Task ChangePasswordAsync(ChangePasswordInputDto changePasswordInputDto);
    Task<User?> GetUserByGuidAsync(Guid userGuid);
    Task UpdateRoommateStatusAsync(UpdateRoommateStatusInputDto updateRoommateStatusInputDto);
    Task<UserProfileDto?> GetUserProfileAsync(int userId);
    Task<UserProfileDto> UpdateUserProfileAsync(int userId, UserProfileUpdateInputDto updateDto);
    Task<UserProfileDto> UpdatePrivacySettingsAsync(int userId, PrivacySettingsDto privacySettingsDto);
    Task<UserExportDto> ExportUserDataAsync(int userId);
    Task UpgradeUserRoleAsync(int userId, string targetRoleName);
    Task SendVerificationEmailAsync(int userId);
    Task<bool> VerifyEmailAsync(string token);
    Task SendPasswordResetEmailAsync(string email);
    Task<bool> ResetPasswordAsync(string token, string newPassword);
}
