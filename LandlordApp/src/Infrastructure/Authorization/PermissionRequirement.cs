using Microsoft.AspNetCore.Authorization;

namespace Lander.src.Infrastructure.Authorization
{
    /// <summary>
    /// Represents a requirement that the user must have a specific permission.
    /// </summary>
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }

        public PermissionRequirement(string permission)
        {
            Permission = permission ?? throw new ArgumentNullException(nameof(permission));
        }
    }
}
