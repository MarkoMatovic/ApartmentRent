using Lander.Helpers;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Modules.Users.Domain.IRepository;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Users.Infrastructure.Repository
{
    public class PermissionRepository(UsersContext context) : IPermissionRepository
    {
        private readonly UsersContext _context = context ?? throw new ArgumentNullException(nameof(context));
        
        public IUnitofWork UnitOfWork => _context;

        public Permission Add(Permission entity)
        {
            return _context.Permissions.Add(entity).Entity;
        }

        public async Task<IEnumerable<Permission>> GetAllPermissionsAsync()
        {
            return await _context.Permissions
                .OrderBy(p => p.PermissionName)
                .ToListAsync();
        }

        public async Task<Permission?> GetPermissionByIdAsync(int permissionId)
        {
            return await _context.Permissions
                .FirstOrDefaultAsync(p => p.PermissionId == permissionId);
        }

        public async Task<Permission?> GetPermissionByNameAsync(string permissionName)
        {
            return await _context.Permissions
                .FirstOrDefaultAsync(p => p.PermissionName == permissionName);
        }

        public async Task<IEnumerable<Permission>> GetPermissionsByRoleIdAsync(int roleId)
        {
            return await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Include(rp => rp.Permission)
                .Select(rp => rp.Permission)
                .ToListAsync();
        }
    }
}
