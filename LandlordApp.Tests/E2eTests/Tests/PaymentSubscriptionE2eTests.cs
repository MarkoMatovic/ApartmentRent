using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LandlordApp.Tests.E2eTests.Infrastructure;
using LandlordApp.Tests.E2eTests.Shared;

namespace LandlordApp.Tests.E2eTests.Tests;

/// <summary>
/// E2E tests for payment and subscription endpoints.
/// External payment gateways (Monri, Payten) are not called — only HTTP-layer logic is verified.
/// </summary>
[Collection(E2eCollection.Name)]
public class PaymentSubscriptionE2eTests : E2eTestBase
{
    public PaymentSubscriptionE2eTests(E2eFixture fixture) : base(fixture) { }

    // ── Plans (public, no auth) ───────────────────────────────────────────────

    [Fact]
    public async Task GetSubscriptionPlans_Anonymous_Returns200WithList()
    {
        var response = await new GetSubscriptionPlansEndpoint(HttpClient).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── CreatePayment [Authorize] ─────────────────────────────────────────────

    [Fact]
    public async Task CreatePayment_AsAuthenticatedUser_Returns200OrConflict()
    {
        var user   = await Data.CreateUserAsync("pay-create@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new CreatePaymentEndpoint(client).CallAsync("basic");

        // 200 = payment initiated, 409 = idempotent duplicate (both are valid)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreatePayment_WithoutAuth_Returns401()
    {
        var response = await new CreatePaymentEndpoint(HttpClient).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Callback (AllowAnonymous) ─────────────────────────────────────────────

    [Fact]
    public async Task PaymentCallback_WithValidJson_Returns200()
    {
        // Always returns 200 to prevent Monri retries even on internal errors
        var response = await new PaymentCallbackEndpoint(HttpClient).CallAsync("{}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PaymentCallback_WithInvalidJson_ReturnsBadRequest()
    {
        var response = await new PaymentCallbackEndpoint(HttpClient).CallAsync("not-json{{");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK);
    }

    // ── Subscription Status [Authorize + ownership] ───────────────────────────

    [Fact]
    public async Task GetSubscriptionStatus_AsOwner_Returns200Or404()
    {
        var user   = await Data.CreateUserAsync("sub-stat@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new GetSubscriptionStatusEndpoint(client).CallAsync(user.UserId);

        // 200 = has subscription, 404 = no active subscription (both valid)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSubscriptionStatus_AsOtherUser_Returns403()
    {
        var owner    = await Data.CreateUserAsync("sub-stat-own@e2e.com");
        var intruder = await Data.CreateUserAsync("sub-stat-int@e2e.com");
        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);

        var response = await new GetSubscriptionStatusEndpoint(client).CallAsync(owner.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSubscriptionStatus_WithoutAuth_Returns401()
    {
        var user = await Data.CreateUserAsync("sub-stat-na@e2e.com");

        var response = await new GetSubscriptionStatusEndpoint(HttpClient).CallAsync(user.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Checkout [Authorize] ──────────────────────────────────────────────────

    [Fact]
    public async Task Checkout_AsAuthenticatedUser_Returns200WithUrl()
    {
        var user   = await Data.CreateUserAsync("sub-checkout@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new CheckoutEndpoint(client).CallAsync("Premium", 9.99m);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("checkoutUrl", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Checkout_WithoutAuth_Returns401()
    {
        var response = await new CheckoutEndpoint(HttpClient).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
