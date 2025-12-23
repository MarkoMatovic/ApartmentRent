using Lander.src.Common;

namespace Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate
{
    public class Permission : IAggregateRoot
    {
        public int PermissionId { get; set; }

        public string PermissionName { get; set; } = null!;

        public string? Description { get; set; }

        public Guid? CreatedByGuid { get; set; }

        public DateTime? CreatedDate { get; set; }

        public Guid? ModifiedByGuid { get; set; }

        public DateTime? ModifiedDate { get; set; }
    }
}
