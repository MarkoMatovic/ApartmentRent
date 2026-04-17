using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Modules.Users.Dtos.Dto;
using Lander.src.Modules.Users.Dtos.InputDto;

namespace Lander.src.Modules.Users.Interfaces.UserInterface;

public interface IUserProfileService
{
    Task<User?> GetUserByGuidAsync(Guid userGuid);
    Task<UserProfileDto?> GetUserProfileAsync(int userId);
    Task<UserProfileDto> UpdateUserProfileAsync(int userId, UserProfileUpdateInputDto dto);
    Task<UserProfileDto> UpdatePrivacySettingsAsync(int userId, PrivacySettingsDto dto);
    Task DeactivateUserAsync(DeactivateUserInputDto dto);
    Task ReactivateUserAsync(ReactivateUserInputDto dto);
    Task<bool> DeleteUserAsync(DeleteUserInputDto dto);
    Task UpdateRoommateStatusAsync(UpdateRoommateStatusInputDto dto);
    Task<UserExportDto> ExportUserDataAsync(int userId);
    Task UpgradeUserRoleAsync(int userId, string targetRoleName);
}
