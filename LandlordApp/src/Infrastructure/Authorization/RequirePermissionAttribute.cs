using Microsoft.AspNetCore.Authorization;

namespace Lander.src.Infrastructure.Authorization
{
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
