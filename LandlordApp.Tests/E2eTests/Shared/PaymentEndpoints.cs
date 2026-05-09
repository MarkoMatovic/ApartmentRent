using System.Net.Http.Json;

namespace LandlordApp.Tests.E2eTests.Shared;

/// <summary>GET /api/payments/plans (public)</summary>
public class GetSubscriptionPlansEndpoint
{
    private readonly HttpClient _client;
    public GetSubscriptionPlansEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync()
        => _client.GetAsync("/api/payments/plans");
}

/// <summary>POST /api/payments/create-payment [Authorize]</summary>
public class CreatePaymentEndpoint
{
    private readonly HttpClient _client;
    public CreatePaymentEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(string planId = "basic",
        string successUrl = "http://localhost/success",
        string failureUrl = "http://localhost/fail")
        => _client.PostAsJsonAsync("/api/payments/create-payment", new
        {
            PlanId     = planId,
            SuccessUrl = successUrl,
            FailureUrl = failureUrl,
        });
}

/// <summary>POST /api/payments/callback (AllowAnonymous)</summary>
public class PaymentCallbackEndpoint
{
    private readonly HttpClient _client;
    public PaymentCallbackEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(string json = "{}")
    {
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        return _client.PostAsync("/api/payments/callback", content);
    }
}

/// <summary>GET /api/subscriptions/status/{userId} [Authorize]</summary>
public class GetSubscriptionStatusEndpoint
{
    private readonly HttpClient _client;
    public GetSubscriptionStatusEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int userId)
        => _client.GetAsync($"/api/subscriptions/status/{userId}");
}

/// <summary>POST /api/subscriptions/checkout [Authorize]</summary>
public class CheckoutEndpoint
{
    private readonly HttpClient _client;
    public CheckoutEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(string planType = "Premium", decimal amount = 9.99m)
        => _client.PostAsJsonAsync("/api/subscriptions/checkout", new
        {
            PlanType = planType,
            Amount   = amount,
        });
}
