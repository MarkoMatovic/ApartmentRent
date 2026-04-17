using System.Collections.Concurrent;
using Lander.src.Common.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Users.Services;

public class UserRoleUpgradeService : IUserRoleUpgradeService
{
    // Per-user semaphore prevents duplicate upgrade writes when a user submits
    // two apartment listings simultaneously (both threads read "Tenant", both would write "TenantLandlord").
    // Static so the lock is effective across all scoped instances within the same process.
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> _upgradeLocks = new();

    private readonly UsersContext _context;
    private readonly ILogger<UserRoleUpgradeService> _logger;
    private readonly TimeProvider _timeProvider;

    public UserRoleUpgradeService(UsersContext context, ILogger<UserRoleUpgradeService> logger, TimeProvider timeProvider)
    {
        _context = context;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task UpgradeAsync(int userId, string targetRoleName)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) throw new NotFoundException("User", userId);

        var targetRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == targetRoleName);
        if (targetRole == null) throw new NotFoundException($"Role '{targetRoleName}' not found");

        user.UserRoleId = targetRole.RoleId;
        user.ModifiedDate = _timeProvider.GetUtcNow().UtcDateTime;
        await _context.SaveEntitiesAsync();
        _logger.LogInformation("User {UserId} upgraded to role {Role}", userId, targetRoleName);
    }

    public async Task AutoUpgradeOnFirstListingAsync(int userId)
    {
        var semaphore = _upgradeLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();
        try
        {
            var user = await _context.Users
                .Include(u => u.UserRole)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            // If already upgraded by a concurrent call that got here first — nothing to do
            if (user?.UserRole?.RoleName != "Tenant") return;

            var tenantLandlordRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == "TenantLandlord");
            if (tenantLandlordRole == null) return;

            user.UserRoleId = tenantLandlordRole.RoleId;
            await _context.SaveEntitiesAsync();
            _logger.LogInformation("Auto-upgraded user {UserId} from Tenant to TenantLandlord", userId);
        }
        finally
        {
            semaphore.Release();
        }
    }
}
