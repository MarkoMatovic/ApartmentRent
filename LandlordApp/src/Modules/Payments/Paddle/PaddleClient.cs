using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Lander.src.Modules.Payments.Paddle;

/// <summary>
/// Thin HTTP client over the Paddle Billing REST API. Registered via the named
/// HttpClient "Paddle"; the base URL and bearer token come from <see cref="PaddleOptions"/>.
/// </summary>
public sealed class PaddleClient : IPaddleClient
{
    private readonly HttpClient _http;
    private readonly PaddleOptions _options;
    private readonly ILogger<PaddleClient> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public PaddleClient(HttpClient http, IOptions<PaddleOptions> options, ILogger<PaddleClient> logger)
    {
        _options = options.Value;
        _logger = logger;
        _http = http;
        _http.BaseAddress ??= new Uri(_options.ApiBaseUrl);
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public async Task<string> CreateTransactionAsync(
        string priceId,
        IReadOnlyDictionary<string, string> customData,
        CancellationToken ct = default)
    {
        // Paddle POST /transactions — a single quantity-1 line for the given price.
        // collection_mode "automatic" charges the customer through checkout immediately.
        var payload = new
        {
            items = new[] { new { price_id = priceId, quantity = 1 } },
            custom_data = customData,
            collection_mode = "automatic"
        };

        using var content = new StringContent(
            JsonSerializer.Serialize(payload, JsonOpts), Encoding.UTF8, "application/json");

        using var resp = await _http.PostAsync("/transactions", content, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("Paddle create-transaction failed ({Status}): {Body}", (int)resp.StatusCode, body);
            throw new PaddleApiException(
                $"Paddle transaction creation failed with status {(int)resp.StatusCode}.");
        }

        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty("data", out var data) &&
            data.TryGetProperty("id", out var id) &&
            id.GetString() is { Length: > 0 } txnId)
        {
            return txnId;
        }

        _logger.LogError("Paddle create-transaction response missing data.id: {Body}", body);
        throw new PaddleApiException("Paddle transaction response did not contain an id.");
    }
}
