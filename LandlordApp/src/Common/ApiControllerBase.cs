using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Lander.src.Common;

/// <summary>
/// Base controller that centralises JWT claim parsing and current-user resolution.
/// Eliminates the repeated GUID-extract → parse → DB-lookup pattern across controllers.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    private readonly IUserInterface _userService;

    protected ApiControllerBase(IUserInterface userService)
    {
        _userService = userService;
    }

    /// <summary>Returns the integer userId from the "userId" claim, or null if absent/unparseable.</summary>
    protected int? TryGetCurrentUserId()
    {
        var claim = User.FindFirstValue("userId");
        return int.TryParse(claim, out var id) ? id : null;
    }

    /// <summary>Returns the user GUID from the "sub" claim, or null if absent/unparseable.</summary>
    protected Guid? TryGetCurrentUserGuid()
    {
        var claim = User.FindFirstValue("sub");
        return Guid.TryParse(claim, out var guid) ? guid : null;
    }

    /// <summary>
    /// Resolves the full User entity from the "sub" claim.
    /// Returns null when the claim is missing or the user does not exist in the DB.
    /// </summary>
    protected async Task<User?> GetCurrentUserAsync()
    {
        var guid = TryGetCurrentUserGuid();
        if (guid is null) return null;
        return await _userService.GetUserByGuidAsync(guid.Value);
    }
}
