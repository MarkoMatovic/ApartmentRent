using Lander.src.Common;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
namespace Lander.src.Modules.Users.Domain.IRepository
{
    public interface IRoleRepository : IRepository<Role>
    {
        Task<Role?> GetRoleById(int RoleId);
    }
}
