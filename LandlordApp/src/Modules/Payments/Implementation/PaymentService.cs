using Lander.Helpers;
using Lander.src.Modules.Payments.Dtos;
using Lander.src.Modules.Payments.Interfaces;
using Lander.src.Modules.Users.Dtos.InputDto;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Payments.Implementation;

/// <summary>
/// Provider-agnostic payment facade. Plan catalogue is sourced from the
/// <c>Payments:Plans</c> configuration section so it is independent of any payment
/// provider. (Plans section is <c>Payments:Plans</c>.)
/// </summary>
public class PaymentService : IPaymentService
{
    private const string PlansSection = "Payments:Plans";

    private readonly IUserInterface _userService;
    private readonly IConfiguration _configuration;
    private readonly PaymentsContext _paymentsContext;
    private readonly UsersContext _usersContext;
    private readonly RoommatesContext _roommatesContext;

    public PaymentService(
        IUserInterface userService,
        IConfiguration configuration,
        PaymentsContext paymentsContext,
        UsersContext usersContext,
        RoommatesContext roommatesContext)
    {
        _userService = userService;
        _configuration = configuration;
        _paymentsContext = paymentsContext;
        _usersContext = usersContext;
        _roommatesContext = roommatesContext;
    }

    public IEnumerable<SubscriptionPlanDto> GetPlans()
        => _configuration.GetSection(PlansSection).GetChildren()
            .Select(s => new SubscriptionPlanDto
            {
                Name        = s["Name"] ?? s.Key,
                Description = s["Description"] ?? string.Empty,
                Price       = decimal.Parse(s["Amount"] ?? "999") / 100,
                Currency    = s["Currency"] ?? "EUR",
                PlanId      = s.Key,
                Interval    = s["Interval"] ?? "month"
            });

    public async Task<UserSubscriptionStatusDto> GetUserStatusAsync(int userId)
    {
        var user = await _usersContext.Users
            .AsNoTracking()
            .Include(u => u.UserRole)
            .Where(u => u.UserId == userId)
            .Select(u => new
            {
                u.HasPersonalAnalytics,
                u.TokenBalance,
                u.ListingCredits,
                u.PriorityInboxUntil,
                RoleName = u.UserRole != null ? u.UserRole.RoleName : null
            })
            .FirstOrDefaultAsync();

        if (user is null)
            return new UserSubscriptionStatusDto();

        var boostedUntil = await GetBoostedUntilAsync(userId);

        return new UserSubscriptionStatusDto
        {
            HasAnalytics       = user.HasPersonalAnalytics,
            TokenBalance       = user.TokenBalance,
            ListingCredits     = user.ListingCredits,
            PriorityInboxUntil = user.PriorityInboxUntil,
            BoostedUntil       = boostedUntil,
            RoleName           = user.RoleName
        };
    }

    public async Task<List<PaymentOrderDto>> GetUserOrdersAsync(int userId)
    {
        // Order numbers are encoded as "{userId}_{planId}_{...}" so we filter by prefix.
        var prefix = $"{userId}_";

        var orders = await _paymentsContext.ProcessedOrders
            .AsNoTracking()
            .Where(o => o.OrderNumber.StartsWith(prefix))
            .OrderByDescending(o => o.ProcessedAt)
            .ToListAsync();

        var planNames = _configuration.GetSection(PlansSection).GetChildren()
            .ToDictionary(s => s.Key, s => s["Name"] ?? s.Key, StringComparer.OrdinalIgnoreCase);

        var planAmounts = _configuration.GetSection(PlansSection).GetChildren()
            .ToDictionary(s => s.Key, s => decimal.Parse(s["Amount"] ?? "0") / 100, StringComparer.OrdinalIgnoreCase);

        return orders.Select(o =>
        {
            var parts = o.OrderNumber.Split('_', 3);
            var planId = parts.Length >= 2 ? parts[1] : o.OrderNumber;
            planNames.TryGetValue(planId, out var planName);
            planAmounts.TryGetValue(planId, out var amount);

            return new PaymentOrderDto
            {
                OrderNumber = o.OrderNumber,
                PlanId      = planId,
                PlanName    = planName ?? planId,
                Amount      = amount,
                Currency    = "EUR",
                ProcessedAt = o.ProcessedAt
            };
        }).ToList();
    }

    public async Task CancelAnalyticsAsync(int userId)
    {
        var userProfile = await _userService.GetUserProfileAsync(userId);
        if (userProfile is null) return;

        var downgradedRole = userProfile.RoleName switch
        {
            RoleConstants.PremiumTenant   => RoleConstants.Tenant,
            RoleConstants.PremiumLandlord => RoleConstants.Landlord,
            _                             => null
        };

        if (downgradedRole != null)
            await _userService.UpgradeUserRoleAsync(userId, downgradedRole);

        var updateDto = new UserProfileUpdateInputDto
        {
            FirstName    = userProfile.FirstName,
            LastName     = userProfile.LastName,
            PhoneNumber  = userProfile.PhoneNumber,
            DateOfBirth  = userProfile.DateOfBirth,
            HasPersonalAnalytics = false
        };
        await _userService.UpdateUserProfileAsync(userId, updateDto);

        // Clear the expiry date so the expiration job doesn't try to process it again.
        await _usersContext.Database.ExecuteSqlRawAsync(
            "UPDATE [UsersRoles].[Users] SET AnalyticsUntil = NULL WHERE UserId = {0}", userId);
    }

    private async Task<DateTime?> GetBoostedUntilAsync(int userId)
    {
        return await _roommatesContext.Roommates
            .AsNoTracking()
            .Where(r => r.UserId == userId && r.IsActive)
            .Select(r => (DateTime?)r.BoostedUntil)
            .FirstOrDefaultAsync();
    }
}
