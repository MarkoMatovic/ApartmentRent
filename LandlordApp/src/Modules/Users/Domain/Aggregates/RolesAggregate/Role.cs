using Lander.src.Common;
namespace Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate
{
    public class Role : IAggregateRoot
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public string? Description { get; set; }
        public Guid? CreatedByGuid { get; set; }
        public DateTime? CreatedDate { get; set; }
        public Guid? ModifiedByGuid { get; set; }
        public DateTime? ModifiedDate { get; set; }
        
        // Navigation properties
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
