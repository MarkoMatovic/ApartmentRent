using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lander.Helpers;
using Lander.src.Modules.Payments.Dtos;
using Lander.src.Modules.Payments.Interfaces;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Lander.src.Modules.Payments.Implementation;

public class MonriService : IMonriService
{
    private static readonly ResiliencePipeline _pipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>().Handle<TimeoutException>()
        })
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            MinimumThroughput = 5,
            BreakDuration = TimeSpan.FromMinutes(1),
            ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>().Handle<TimeoutException>()
        })
        .Build();

    // Idempotency: track recently processed order numbers (order_number → processed-at)
    private static readonly ConcurrentDictionary<string, DateTimeOffset> _processedOrders = new();

    private readonly string _authenticityToken;
    private readonly string _merchantKey;
    private readonly string _baseUrl;
    private readonly string _callbackBaseUrl;
    private readonly IUserInterface _userService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MonriService> _logger;

    public MonriService(IConfiguration configuration, IUserInterface userService, ILogger<MonriService> logger)
    {
        _authenticityToken = configuration["Monri:AuthenticityToken"] ?? throw new InvalidOperationException("Monri:AuthenticityToken is not configured");
        _merchantKey = configuration["Monri:MerchantKey"] ?? throw new InvalidOperationException("Monri:MerchantKey is not configured");
        _baseUrl = configuration["Monri:BaseUrl"] ?? "https://ipgtest.monri.com";
        _callbackBaseUrl = configuration["Monri:CallbackBaseUrl"] ?? "https://localhost:7092";
        _userService = userService;
        _configuration = configuration;
        _logger = logger;
    }

    public MonriPaymentFormDto CreatePaymentForm(
        string planId,
        string successUrl,
        string failureUrl,
        int userId,
        string buyerEmail,
        string buyerName)
    {
        ValidateRedirectUrl(successUrl, nameof(successUrl));
        ValidateRedirectUrl(failureUrl, nameof(failureUrl));

        var plan = GetPlan(planId);

        // Order number encodes the userId and planId so we can recover them in the callback
        // Format: userId_planId_timestamp  e.g. "42_personal_analytics_20240401143022"
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var orderNumber = $"{userId}_{planId}_{timestamp}";

        var digest = CalculateDigest(_merchantKey, orderNumber, plan.Amount, plan.Currency);

        return new MonriPaymentFormDto
        {
            FormAction = $"{_baseUrl}/v2/form",
            AuthenticityToken = _authenticityToken,
            OrderNumber = orderNumber,
            Amount = plan.Amount,
            Currency = plan.Currency,
            OrderInfo = plan.Name,
            Digest = digest,
            SuccessUrl = successUrl,
            FailureUrl = failureUrl,
            CallbackUrl = $"{_callbackBaseUrl}/api/payments/callback",
            BuyerEmail = buyerEmail,
            BuyerName = buyerName
        };
    }

    public async Task HandleCallbackAsync(string json)
    {
        await _pipeline.ExecuteAsync(async ct => await HandleCallbackInternalAsync(json, ct));
    }

    private async Task HandleCallbackInternalAsync(string json, CancellationToken ct)
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

        // Monri v2 form callback: { "event": "transaction:approved", "payload": { ... } }
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
        // Also handle direct form callback (response_code field at root level)
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
    /// Validates the Monri callback HMAC digest.
    /// Formula: SHA512(merchant_key + order_number + pgw_transaction_id + pgw_response_code
    ///                  + pgw_amount + pgw_outgoing_amount + pgw_currency + pgw_outgoing_currency
    ///                  + pgw_approval_code + pgw_response_message)
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

            string Get(string key) =>
                data.TryGetProperty(key, out var v) ? v.GetString() ?? "" : "";

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

            var expected = Convert.ToHexString(
                SHA512.HashData(Encoding.UTF8.GetBytes(raw))
            ).ToLower();

            var match = string.Equals(expected, receivedDigest, StringComparison.OrdinalIgnoreCase);
            if (!match)
                _logger.LogWarning(
                    "Monri digest mismatch. Expected: {Expected}, Received: {Received}",
                    expected[..16] + "…", receivedDigest[..16] + "…");

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
        if (string.IsNullOrEmpty(orderNumber))
            return;

        // Idempotency: reject duplicate callbacks for the same order
        var now = DateTimeOffset.UtcNow;
        if (!_processedOrders.TryAdd(orderNumber, now))
        {
            _logger.LogWarning("Monri duplicate callback for order {OrderNumber} — ignored", orderNumber);
            return;
        }
        // Evict entries older than 24 hours to prevent unbounded growth
        foreach (var key in _processedOrders.Keys)
            if (_processedOrders.TryGetValue(key, out var ts) && (now - ts).TotalHours > 24)
                _processedOrders.TryRemove(key, out _);

        // Decode userId and planId from order number: "{userId}_{planId}_{timestamp}"
        var parts = orderNumber.Split('_', 3);
        if (parts.Length < 3 || !int.TryParse(parts[0], out var userId))
            return;

        var planId = parts[1];

        var userProfile = await _userService.GetUserProfileAsync(userId);
        if (userProfile == null)
            return;

        string targetRoleName = userProfile.RoleName switch
        {
            RoleConstants.Tenant         => RoleConstants.PremiumTenant,
            RoleConstants.TenantLandlord => RoleConstants.PremiumLandlord,
            _                            => RoleConstants.PremiumLandlord
        };

        _logger.LogInformation("Monri payment approved — upgrading user {UserId} to {Role} for plan {PlanId}", userId, targetRoleName, planId);
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
    }

    // Digest = SHA512(merchant_key + order_number + amount + currency)
    private static string CalculateDigest(string merchantKey, string orderNumber, long amount, string currency)
    {
        var data = $"{merchantKey}{orderNumber}{amount}{currency}";
        var hashBytes = SHA512.HashData(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hashBytes).ToLower();
    }

    private MonriPlan GetPlan(string planId)
    {
        var section = _configuration.GetSection($"Monri:Plans:{planId}");
        if (!section.Exists())
            throw new ArgumentException($"Plan '{planId}' not found in configuration");

        return new MonriPlan
        {
            Name = section["Name"] ?? planId,
            Amount = long.Parse(section["Amount"] ?? "999"),
            Currency = section["Currency"] ?? "EUR"
        };
    }

    private void ValidateRedirectUrl(string url, string paramName)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new ArgumentException($"Invalid URL for {paramName}: '{url}'");

        var allowedHosts = _configuration.GetSection("Monri:AllowedRedirectHosts").Get<string[]>()
                           ?? Array.Empty<string>();

        if (allowedHosts.Length > 0 && !allowedHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"Redirect host '{uri.Host}' is not allowed for {paramName}.");
    }

    private sealed class MonriPlan
    {
        public string Name { get; set; } = string.Empty;
        public long Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
    }
}
