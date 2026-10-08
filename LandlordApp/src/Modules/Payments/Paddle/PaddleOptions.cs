using System.ComponentModel.DataAnnotations;

namespace Lander.src.Modules.Payments.Paddle;

/// <summary>
/// Paddle Billing configuration. Bound from the "Paddle" section.
/// Secrets (ApiKey, WebhookSecret) come from environment variables / user-secrets
/// in production — never commit them. ClientToken is public (safe for the browser).
/// </summary>
public sealed class PaddleOptions
{
    public const string SectionName = "Paddle";

    /// <summary>"sandbox" (default, for development/testing) or "production".</summary>
    public string Environment { get; set; } = "sandbox";

    /// <summary>Server-side API key (secret, prefix <c>pdl_...</c>) used to create transactions.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Client-side token (public, prefix <c>test_/live_</c>) used by Paddle.js in the browser.</summary>
    public string ClientToken { get; set; } = string.Empty;

    /// <summary>Notification-destination secret (prefix <c>pdl_ntfset_...</c>) used to verify webhook signatures.</summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// Maps our internal planId (e.g. "tokens-50") to the Paddle Price ID (e.g. "pri_...").
    /// Every purchasable plan must have an entry before it can be bought.
    /// </summary>
    public Dictionary<string, string> PriceIds { get; set; } = new();

    /// <summary>
    /// Reverse lookup Paddle Price ID → our planId. Used by the webhook to derive the
    /// plan from the actually-purchased price, so fulfillment never trusts a client-supplied planId.
    /// </summary>
    public Dictionary<string, string> PlanIdByPriceId =>
        PriceIds
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .GroupBy(kv => kv.Value)
            .ToDictionary(g => g.Key, g => g.First().Key);

    /// <summary>
    /// Maximum age (seconds) of a webhook's signed timestamp before it is rejected as a
    /// potential replay. Paddle's own SDKs default to 5s; widen if clock skew causes false
    /// rejects. A legitimately retried webhook carries a fresh timestamp, so this only
    /// rejects replays of captured request bytes.
    /// </summary>
    public int MaxWebhookAgeSeconds { get; set; } = 5;

    /// <summary>
    /// True when checkout can be created and opened: needs the server API key (to create
    /// the transaction) and the client token (for Paddle.js). The WebhookSecret is NOT
    /// required here — it only gates fulfillment in the webhook handler, so you can test
    /// the checkout UI before wiring up the webhook/tunnel.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApiKey) &&
        !string.IsNullOrWhiteSpace(ClientToken);

    /// <summary>API base URL for server-to-server calls, derived from <see cref="Environment"/>.</summary>
    public string ApiBaseUrl =>
        Environment.Equals("production", StringComparison.OrdinalIgnoreCase)
            ? "https://api.paddle.com"
            : "https://sandbox-api.paddle.com";
}
