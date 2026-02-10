using Lander.src.Modules.Users.Domain.IRepository;
using Lander.src.Modules.Users.Domain.IService;
using Lander.src.Modules.Users.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Users.Implementation.PermissionImplementation
{
    public class PermissionService(IPermissionRepository permissionRepository, UsersContext context) : IPermissionService
    {
        private readonly IPermissionRepository _permissionRepository = permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
        private readonly UsersContext _context = context ?? throw new ArgumentNullException(nameof(context));

        public async Task<IEnumerable<PermissionDto>> GetAllPermissionsAsync()
        {
            var permissions = await _permissionRepository.GetAllPermissionsAsync();
            return permissions.Select(p => new PermissionDto
            {
                PermissionId = p.PermissionId,
                PermissionName = p.PermissionName,
                Description = p.Description
            });
        }

        public async Task<PermissionDto?> GetPermissionByIdAsync(int permissionId)
        {
            var permission = await _permissionRepository.GetPermissionByIdAsync(permissionId);
            if (permission == null) return null;

            return new PermissionDto
            {
                PermissionId = permission.PermissionId,
                PermissionName = permission.PermissionName,
                Description = permission.Description
            };
        }

        public async Task<IEnumerable<PermissionDto>> GetPermissionsByRoleIdAsync(int roleId)
        {
            var permissions = await _permissionRepository.GetPermissionsByRoleIdAsync(roleId);
            return permissions.Select(p => new PermissionDto
            {
                PermissionId = p.PermissionId,
                PermissionName = p.PermissionName,
                Description = p.Description
            });
        }

        public async Task<IEnumerable<PermissionDto>> GetPermissionsByUserIdAsync(int userId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);
            
            if (user?.UserRoleId == null) return Enumerable.Empty<PermissionDto>();

            var permissions = await _permissionRepository.GetPermissionsByRoleIdAsync(user.UserRoleId.Value);
            return permissions.Select(p => new PermissionDto
            {
                PermissionId = p.PermissionId,
                PermissionName = p.PermissionName,
                Description = p.Description
            });
        }
    }
}
