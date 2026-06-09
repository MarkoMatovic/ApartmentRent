using Lander.Helpers;
using Lander.src.Modules.Payments.Dtos;
using Lander.src.Modules.Payments.Interfaces;
using Lander.src.Modules.Payments.Models;
using Lander.src.Modules.Users.Dtos.InputDto;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Payments.Implementation;

/// <summary>
/// Facade that keeps IMonriService intact for controllers while delegating to focused sub-services.
/// </summary>
public class MonriService : IMonriService
{
    private readonly IMonriPaymentFormService _formService;
    private readonly IMonriCallbackHandler _callbackHandler;
    private readonly IUserInterface _userService;
    private readonly IdempotencyService _idempotencyService;
    private readonly IConfiguration _configuration;
    private readonly PaymentsContext _paymentsContext;
    private readonly UsersContext _usersContext;
    private readonly RoommatesContext _roommatesContext;

    public MonriService(
        IMonriPaymentFormService formService,
        IMonriCallbackHandler callbackHandler,
        IUserInterface userService,
        IdempotencyService idempotencyService,
        IConfiguration configuration,
        PaymentsContext paymentsContext,
        UsersContext usersContext,
        RoommatesContext roommatesContext)
    {
        _formService = formService;
        _callbackHandler = callbackHandler;
        _userService = userService;
        _idempotencyService = idempotencyService;
        _configuration = configuration;
        _paymentsContext = paymentsContext;
        _usersContext = usersContext;
        _roommatesContext = roommatesContext;
    }

    public MonriPaymentFormDto CreatePaymentForm(
        string planId, string successUrl, string failureUrl,
        int userId, string buyerEmail, string buyerName)
        => _formService.CreatePaymentForm(planId, successUrl, failureUrl, userId, buyerEmail, buyerName);

    public MonriPaymentFormDto CreatePaymentForm(
        string planId, string successUrl, string failureUrl,
        int userId, string buyerEmail, string buyerName,
        int? apartmentId)
        => _formService.CreatePaymentForm(planId, successUrl, failureUrl, userId, buyerEmail, buyerName, apartmentId);

    public async Task<MonriPaymentFormDto?> CreatePaymentAsync(
        int userId, string planId, string successUrl, string failureUrl,
        string? idempotencyKey = null, int? apartmentId = null)
    {
        if (idempotencyKey is not null &&
            await _idempotencyService.IsDuplicateAsync($"payment:{userId}:{idempotencyKey}"))
            return null;

        var userProfile = await _userService.GetUserProfileAsync(userId);
        if (userProfile == null)
            throw new InvalidOperationException("Korisnički profil nije pronađen.");

        var buyerEmail = userProfile.Email;
        var buyerName  = $"{userProfile.FirstName} {userProfile.LastName}".Trim();

        return _formService.CreatePaymentForm(planId, successUrl, failureUrl, userId, buyerEmail, buyerName, apartmentId);
    }

    public IEnumerable<SubscriptionPlanDto> GetPlans()
        => _configuration.GetSection("Monri:Plans").GetChildren()
            .Select(s => new SubscriptionPlanDto
            {
                Name        = s["Name"] ?? s.Key,
                Description = s["Description"] ?? string.Empty,
                Price       = decimal.Parse(s["Amount"] ?? "999") / 100,
                Currency    = s["Currency"] ?? "EUR",
                PlanId      = s.Key,
                Interval    = s["Interval"] ?? "month"
            });

    public Task HandleCallbackAsync(string json)
        => _callbackHandler.HandleCallbackAsync(json);

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

        // BoostedUntil lives on the Roommate profile (separate context) — query separately
        var boostedUntil = await GetBoostedUntilAsync(userId);

        return new UserSubscriptionStatusDto
        {
            HasAnalytics      = user.HasPersonalAnalytics,
            TokenBalance      = user.TokenBalance,
            ListingCredits    = user.ListingCredits,
            PriorityInboxUntil = user.PriorityInboxUntil,
            BoostedUntil      = boostedUntil,
            RoleName          = user.RoleName
        };
    }

    public async Task<List<PaymentOrderDto>> GetUserOrdersAsync(int userId)
    {
        // Order numbers are encoded as "{userId}_{planId}_{...}" so we filter by prefix.
        var prefix = $"{userId}_";

        var orders = await _paymentsContext.ProcessedMonriOrders
            .AsNoTracking()
            .Where(o => o.OrderNumber.StartsWith(prefix))
            .OrderByDescending(o => o.ProcessedAt)
            .ToListAsync();

        // Build plan name lookup from configuration
        var planNames = _configuration.GetSection("Monri:Plans").GetChildren()
            .ToDictionary(
                s => s.Key,
                s => s["Name"] ?? s.Key,
                StringComparer.OrdinalIgnoreCase);

        var planAmounts = _configuration.GetSection("Monri:Plans").GetChildren()
            .ToDictionary(
                s => s.Key,
                s => decimal.Parse(s["Amount"] ?? "0") / 100,
                StringComparer.OrdinalIgnoreCase);

        return orders.Select(o =>
        {
            // Parse planId from order number: "{userId}_{planId}_{...}"
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

        // Downgrade role: Premium → basic equivalent
        var downgradedRole = userProfile.RoleName switch
        {
            RoleConstants.PremiumTenant   => RoleConstants.Tenant,
            RoleConstants.PremiumLandlord => RoleConstants.Landlord,
            _                             => null
        };

        if (downgradedRole != null)
            await _userService.UpgradeUserRoleAsync(userId, downgradedRole);

        // Clear analytics flag
        var updateDto = new UserProfileUpdateInputDto
        {
            FirstName    = userProfile.FirstName,
            LastName     = userProfile.LastName,
            Email        = userProfile.Email,
            PhoneNumber  = userProfile.PhoneNumber,
            DateOfBirth  = userProfile.DateOfBirth,
            HasPersonalAnalytics = false
        };
        await _userService.UpdateUserProfileAsync(userId, updateDto);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<DateTime?> GetBoostedUntilAsync(int userId)
    {
        return await _roommatesContext.Roommates
            .AsNoTracking()
            .Where(r => r.UserId == userId && r.IsActive)
            .Select(r => (DateTime?)r.BoostedUntil)
            .FirstOrDefaultAsync();
    }
}
