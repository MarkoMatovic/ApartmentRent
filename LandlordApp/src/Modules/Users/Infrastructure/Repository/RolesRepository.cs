using Lander.Helpers;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Modules.Users.Domain.IRepository;
using Microsoft.EntityFrameworkCore;
namespace Lander.src.Modules.Users.Infrastructure.Repository
{
    public class RolesRepository(UsersContext context) : IRoleRepository
    {
        #region Properties
        private readonly UsersContext _context = context ?? throw new ArgumentNullException(nameof(context));
        public IUnitOfWork UnitOfWork => _context;
        public Role Add(Role entity)
        {
            return _context.Roles.Add(entity).Entity;
        }
        public async Task<Role?> GetRoleById(int RoleId)
        {
            return await _context.Roles.SingleOrDefaultAsync(role => role.RoleId == RoleId);
        }
        #endregion
    }
}
