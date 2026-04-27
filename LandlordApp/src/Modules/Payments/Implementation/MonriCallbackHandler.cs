using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lander.Helpers;
using Lander.src.Modules.Payments.Interfaces;
using Lander.src.Modules.Payments.Models;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Payments.Implementation;

public class MonriCallbackHandler : IMonriCallbackHandler
{
    private readonly string _merchantKey;
    private readonly IUserInterface _userService;
    private readonly PaymentsContext _paymentsContext;
    private readonly ILogger<MonriCallbackHandler> _logger;
    private readonly TimeProvider _timeProvider;

    public MonriCallbackHandler(
        IConfiguration configuration,
        IUserInterface userService,
        PaymentsContext paymentsContext,
        ILogger<MonriCallbackHandler> logger,
        TimeProvider timeProvider)
    {
        _merchantKey = configuration["Monri:MerchantKey"]
            ?? throw new InvalidOperationException("Monri:MerchantKey is not configured");
        _userService = userService;
        _paymentsContext = paymentsContext;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task HandleCallbackAsync(string json)
    {
        _logger.LogInformation("Monri callback received");
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Monri callback contained invalid JSON");
            throw new ArgumentException("Invalid callback JSON");
        }

        var root = doc.RootElement;
        var eventType = root.TryGetProperty("event", out var ev) ? ev.GetString() : null;
        var hasPayload = root.TryGetProperty("payload", out var payload);

        if (eventType == "transaction:approved" && hasPayload)
        {
            if (!ValidateCallbackDigest(payload))
            {
                _logger.LogWarning("Monri callback HMAC digest validation failed — payload rejected");
                throw new InvalidOperationException("Monri callback digest mismatch.");
            }
            await ProcessApprovedTransaction(payload);
        }
        else if (root.TryGetProperty("pgw_response_code", out var pgwCode) && pgwCode.GetString() == "0000")
        {
            if (!ValidateCallbackDigest(root))
            {
                _logger.LogWarning("Monri form callback HMAC digest validation failed — payload rejected");
                throw new InvalidOperationException("Monri callback digest mismatch.");
            }
            await ProcessApprovedTransaction(root);
        }
    }

    /// <summary>
    /// Validates Monri callback HMAC:
    /// SHA512(merchant_key + order_number + pgw_transaction_id + pgw_response_code
    ///        + pgw_amount + pgw_outgoing_amount + pgw_currency + pgw_outgoing_currency
    ///        + pgw_approval_code + pgw_response_message)
    /// </summary>
    private bool ValidateCallbackDigest(JsonElement data)
    {
        try
        {
            var receivedDigest = data.TryGetProperty("digest", out var d) ? d.GetString() : null;
            if (string.IsNullOrEmpty(receivedDigest))
            {
                _logger.LogWarning("Monri callback missing digest field");
                return false;
            }

            string Get(string key) => data.TryGetProperty(key, out var v) ? v.GetString() ?? "" : "";

            var raw = string.Concat(
                _merchantKey,
                Get("order_number"),
                Get("pgw_transaction_id"),
                Get("pgw_response_code"),
                Get("pgw_amount"),
                Get("pgw_outgoing_amount"),
                Get("pgw_currency"),
                Get("pgw_outgoing_currency"),
                Get("pgw_approval_code"),
                Get("pgw_response_message")
            );

            var expected = Convert.ToHexString(SHA512.HashData(Encoding.UTF8.GetBytes(raw))).ToLower();
            var match = string.Equals(expected, receivedDigest, StringComparison.OrdinalIgnoreCase);
            if (!match)
                _logger.LogWarning("Monri digest mismatch. Expected: {Expected}, Received: {Received}",
                    (expected.Length >= 16 ? expected[..16] : expected) + "…",
                    (receivedDigest.Length >= 16 ? receivedDigest[..16] : receivedDigest) + "…");
            return match;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Monri digest validation");
            return false;
        }
    }

    private async Task ProcessApprovedTransaction(JsonElement data)
    {
        var orderNumber = data.TryGetProperty("order_number", out var on) ? on.GetString() : null;
        if (string.IsNullOrEmpty(orderNumber)) return;

        // DB-backed idempotency — safe across multiple instances
        var alreadyProcessed = await _paymentsContext.ProcessedMonriOrders
            .AnyAsync(o => o.OrderNumber == orderNumber);
        if (alreadyProcessed)
        {
            _logger.LogWarning("Monri duplicate callback for order {OrderNumber} — ignored", orderNumber);
            return;
        }

        // Decode userId and planId from order number: "{userId}_{planId}_{timestamp}"
        var parts = orderNumber.Split('_', 3);
        if (parts.Length < 3 || !int.TryParse(parts[0], out var userId)) return;

        var planId = parts[1];
        var userProfile = await _userService.GetUserProfileAsync(userId);
        if (userProfile == null) return;

        string targetRoleName = userProfile.RoleName switch
        {
            RoleConstants.Tenant         => RoleConstants.PremiumTenant,
            RoleConstants.TenantLandlord => RoleConstants.PremiumLandlord,
            _                            => RoleConstants.PremiumLandlord
        };

        _logger.LogInformation("Monri payment approved — upgrading user {UserId} to {Role} for plan {PlanId}",
            userId, targetRoleName, planId);
        await _userService.UpgradeUserRoleAsync(userId, targetRoleName);

        var updateDto = new Lander.src.Modules.Users.Dtos.InputDto.UserProfileUpdateInputDto
        {
            FirstName = userProfile.FirstName,
            LastName = userProfile.LastName,
            Email = userProfile.Email,
            PhoneNumber = userProfile.PhoneNumber,
            DateOfBirth = userProfile.DateOfBirth,
            HasPersonalAnalytics = true
        };
        await _userService.UpdateUserProfileAsync(userId, updateDto);

        // Mark as processed AFTER upgrade succeeds — safe to retry if this save fails
        try
        {
            _paymentsContext.ProcessedMonriOrders.Add(new ProcessedMonriOrder
            {
                OrderNumber = orderNumber,
                ProcessedAt = _timeProvider.GetUtcNow()
            });
            await _paymentsContext.SaveEntitiesAsync();
        }
        catch (DbUpdateException)
        {
            // Unique constraint race — another instance processed it concurrently; upgrade already done
            _logger.LogWarning("Monri concurrent duplicate callback for order {OrderNumber} — upgrade already applied", orderNumber);
        }
    }
}
