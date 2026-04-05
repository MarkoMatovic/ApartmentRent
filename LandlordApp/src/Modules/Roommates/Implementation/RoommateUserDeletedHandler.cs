using Lander.src.Common;
using Lander.src.Modules.Roommates.Interfaces;

namespace Lander.src.Modules.Roommates.Implementation;

public class RoommateUserDeletedHandler : IUserDeletedHandler
{
    private readonly IRoommateService _roommateService;
    public RoommateUserDeletedHandler(IRoommateService roommateService) => _roommateService = roommateService;
    public Task HandleAsync(int userId) => _roommateService.DeleteRoommateByUserIdAsync(userId);
}
