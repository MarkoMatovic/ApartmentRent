using System.Security.Claims;
using Lander.src.Modules.Listings.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Infrastructure.Authorization;

public class ApartmentOwnerHandler : AuthorizationHandler<ApartmentOwnerRequirement, Apartment>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ApartmentOwnerHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ApartmentOwnerRequirement requirement,
        Apartment apartment)
    {
        var userGuidStr = context.User.FindFirstValue("sub")
            ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userGuidStr == null || !Guid.TryParse(userGuidStr, out var userGuid))
            return;

        using var scope = _scopeFactory.CreateScope();
        var usersContext = scope.ServiceProvider.GetRequiredService<UsersContext>();

        var userId = await usersContext.Users
            .AsNoTracking()
            .Where(u => u.UserGuid == userGuid)
            .Select(u => (int?)u.UserId)
            .FirstOrDefaultAsync();

        if (userId.HasValue && apartment.LandlordId == userId.Value)
            context.Succeed(requirement);
    }
}
