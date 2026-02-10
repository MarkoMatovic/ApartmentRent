using Lander.src.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate
{
    /// <summary>
    /// Junction table for many-to-many relationship between Roles and Permissions.
    /// </summary>
    [PrimaryKey(nameof(RoleId), nameof(PermissionId))]
    public class RolePermission : IAggregateRoot
    {
        public int RoleId { get; set; }
        public int PermissionId { get; set; }
        public Guid? CreatedByGuid { get; set; }
        public DateTime? CreatedDate { get; set; }

        // Navigation properties
        public virtual Role Role { get; set; } = null!;
        public virtual Permission Permission { get; set; } = null!;
    }
}
