using Lander.src.Common;
using Lander.src.Modules.Roommates.Interfaces;

namespace Lander.src.Modules.Roommates.Implementation;

public class RoommateUserDeletedHandler : IUserDeletedHandler
{
    private readonly IRoommateService _roommateService;
    private readonly IRoommatePairingService _pairingService;

    public RoommateUserDeletedHandler(IRoommateService roommateService, IRoommatePairingService pairingService)
    {
        _roommateService = roommateService;
        _pairingService = pairingService;
    }

    public async Task HandleAsync(int userId)
    {
        await _roommateService.DeleteRoommateByUserIdAsync(userId);
        await _pairingService.EndAllForUserAsync(userId);
    }
}
