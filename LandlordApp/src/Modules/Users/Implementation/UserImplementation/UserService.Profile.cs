using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Modules.Users.Dtos.Dto;
using Lander.src.Modules.Users.Dtos.InputDto;

namespace Lander.src.Modules.Users.Implementation.UserImplementation;

public partial class UserService
{
    public Task<User?> GetUserByGuidAsync(Guid userGuid)
        => _profileService.GetUserByGuidAsync(userGuid);

    public Task<UserProfileDto?> GetUserProfileAsync(int userId)
        => _profileService.GetUserProfileAsync(userId);

    public Task<UserProfileDto> UpdateUserProfileAsync(int userId, UserProfileUpdateInputDto dto)
        => _profileService.UpdateUserProfileAsync(userId, dto);

    public Task<UserProfileDto> UpdatePrivacySettingsAsync(int userId, PrivacySettingsDto dto)
        => _profileService.UpdatePrivacySettingsAsync(userId, dto);

    public Task DeactivateUserAsync(DeactivateUserInputDto dto)
        => _profileService.DeactivateUserAsync(dto);

    public Task ReactivateUserAsync(ReactivateUserInputDto dto)
        => _profileService.ReactivateUserAsync(dto);

    public Task<bool> DeleteUserAsync(DeleteUserInputDto dto)
        => _profileService.DeleteUserAsync(dto);

    public Task UpdateRoommateStatusAsync(UpdateRoommateStatusInputDto dto)
        => _profileService.UpdateRoommateStatusAsync(dto);

    public Task<UserExportDto> ExportUserDataAsync(int userId)
        => _profileService.ExportUserDataAsync(userId);

    public Task UpgradeUserRoleAsync(int userId, string targetRoleName)
        => _profileService.UpgradeUserRoleAsync(userId, targetRoleName);
}
