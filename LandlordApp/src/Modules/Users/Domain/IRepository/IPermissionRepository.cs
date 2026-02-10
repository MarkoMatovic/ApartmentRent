using Lander.src.Common;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;

namespace Lander.src.Modules.Users.Domain.IRepository
{
    public interface IPermissionRepository : IRepository<Permission>
    {
        Task<IEnumerable<Permission>> GetAllPermissionsAsync();
        Task<Permission?> GetPermissionByIdAsync(int permissionId);
        Task<Permission?> GetPermissionByNameAsync(string permissionName);
        Task<IEnumerable<Permission>> GetPermissionsByRoleIdAsync(int roleId);
    }
}
