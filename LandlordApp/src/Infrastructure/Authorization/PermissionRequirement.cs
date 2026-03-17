using Microsoft.AspNetCore.Authorization;

namespace Lander.src.Infrastructure.Authorization
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }

        public PermissionRequirement(string permission)
        {
            Permission = permission ?? throw new ArgumentNullException(nameof(permission));
        }
    }
}
