using Microsoft.AspNetCore.Authorization;

namespace Lander.src.Infrastructure.Authorization
{
    /// <summary>
    /// Handles permission-based authorization by checking if the user's claims include the required permission.
    /// </summary>
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            // Check if user has the permission claim
            if (context.User.HasClaim(c => c.Type == "permission" && c.Value == requirement.Permission))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
