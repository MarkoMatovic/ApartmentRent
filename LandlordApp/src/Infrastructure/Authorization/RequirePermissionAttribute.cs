using Microsoft.AspNetCore.Authorization;

namespace Lander.src.Infrastructure.Authorization
{
    /// <summary>
    /// Authorization attribute to require specific permissions for controller actions.
    /// Usage: [RequirePermission("apartments.create")]
    /// </summary>
    public class RequirePermissionAttribute : AuthorizeAttribute
    {
        public const string PolicyPrefix = "RequirePermission:";

        public RequirePermissionAttribute(string permission)
        {
            Permission = permission;
            Policy = $"{PolicyPrefix}{permission}";
        }

        public string Permission { get; }
    }
}
