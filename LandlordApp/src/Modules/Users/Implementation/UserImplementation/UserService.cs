using Lander.src.Modules.Users.Interfaces.UserInterface;

namespace Lander.src.Modules.Users.Implementation.UserImplementation;

/// <summary>
/// Facade that implements IUserInterface by delegating to focused sub-services.
/// Controllers and external code depend only on this contract — sub-services are internal detail.
/// </summary>
public partial class UserService : IUserInterface
{
    private readonly IAuthService _authService;
    private readonly IPasswordService _passwordService;
    private readonly IUserProfileService _profileService;

    public UserService(
        IAuthService authService,
        IPasswordService passwordService,
        IUserProfileService profileService)
    {
        _authService = authService;
        _passwordService = passwordService;
        _profileService = profileService;
    }
}
