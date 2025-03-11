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

        public IUnitofWork UnitOfWork => throw new NotImplementedException();

        public Role Add(Role entity)
        {
            throw new NotImplementedException();
        }

        public async Task<Role?> GetRoleById(int RoleId)
        {
            return await _context.Roles.SingleOrDefaultAsync(role => role.RoleId == RoleId);
        }
        #endregion
    }
}
