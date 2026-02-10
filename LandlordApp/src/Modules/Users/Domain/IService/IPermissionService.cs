using Lander.src.Modules.Users.Dtos;

namespace Lander.src.Modules.Users.Domain.IService
{
    public interface IPermissionService
    {
        Task<IEnumerable<PermissionDto>> GetAllPermissionsAsync();
        Task<PermissionDto?> GetPermissionByIdAsync(int permissionId);
        Task<IEnumerable<PermissionDto>> GetPermissionsByRoleIdAsync(int roleId);
        Task<IEnumerable<PermissionDto>> GetPermissionsByUserIdAsync(int userId);
    }
}
