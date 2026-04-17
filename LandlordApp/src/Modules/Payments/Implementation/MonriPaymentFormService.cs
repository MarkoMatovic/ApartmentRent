using System.Security.Cryptography;
using System.Text;
using Lander.src.Modules.Payments.Dtos;
using Lander.src.Modules.Payments.Interfaces;

namespace Lander.src.Modules.Payments.Implementation;

public class MonriPaymentFormService : IMonriPaymentFormService
{
    private readonly string _authenticityToken;
    private readonly string _merchantKey;
    private readonly string _baseUrl;
    private readonly string _callbackBaseUrl;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MonriPaymentFormService> _logger;

    public MonriPaymentFormService(IConfiguration configuration, ILogger<MonriPaymentFormService> logger)
    {
        _authenticityToken = configuration["Monri:AuthenticityToken"]
            ?? throw new InvalidOperationException("Monri:AuthenticityToken is not configured");
        _merchantKey = configuration["Monri:MerchantKey"]
            ?? throw new InvalidOperationException("Monri:MerchantKey is not configured");
        _baseUrl = configuration["Monri:BaseUrl"] ?? "https://ipgtest.monri.com";
        _callbackBaseUrl = configuration["Monri:CallbackBaseUrl"] ?? "https://localhost:7092";
        _configuration = configuration;
        _logger = logger;
    }

    public MonriPaymentFormDto CreatePaymentForm(
        string planId, string successUrl, string failureUrl,
        int userId, string buyerEmail, string buyerName)
    {
        ValidateRedirectUrl(successUrl, nameof(successUrl));
        ValidateRedirectUrl(failureUrl, nameof(failureUrl));

        var plan = GetPlan(planId);
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

    private static string CalculateDigest(string merchantKey, string orderNumber, long amount, string currency)
    {
        var data = $"{merchantKey}{orderNumber}{amount}{currency}";
        return Convert.ToHexString(SHA512.HashData(Encoding.UTF8.GetBytes(data))).ToLower();
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
