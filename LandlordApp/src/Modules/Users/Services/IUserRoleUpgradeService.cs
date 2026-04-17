namespace Lander.src.Modules.Users.Services;

public interface IUserRoleUpgradeService
{
    Task UpgradeAsync(int userId, string targetRoleName);
    Task AutoUpgradeOnFirstListingAsync(int userId);
}
