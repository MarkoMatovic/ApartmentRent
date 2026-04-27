using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Lander.src.Common;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    private readonly IUserInterface _userService;

    protected ApiControllerBase(IUserInterface userService)
    {
        _userService = userService;
    }

    protected int? TryGetCurrentUserId()
    {
        var claim = User.FindFirstValue("userId");
        return int.TryParse(claim, out var id) ? id : null;
    }

    protected Guid? TryGetCurrentUserGuid()
    {
        var claim = User.FindFirstValue("sub");
        return Guid.TryParse(claim, out var guid) ? guid : null;
    }

    protected async Task<User?> GetCurrentUserAsync()
    {
        var guid = TryGetCurrentUserGuid();
        if (guid is null) return null;
        return await _userService.GetUserByGuidAsync(guid.Value);
    }
}
