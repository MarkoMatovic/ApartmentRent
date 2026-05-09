using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LandlordApp.Tests.E2eTests.Infrastructure;
using LandlordApp.Tests.E2eTests.Shared;

namespace LandlordApp.Tests.E2eTests.Tests;

/// <summary>
/// E2E tests for the tenant-applies-landlord-reviews application flow.
///
/// Happy path:
///   Tenant applies → Landlord sees application → Landlord approves → Tenant checks status
///
/// Negative cases:
///   Duplicate application, unauthorized access, role guards.
/// </summary>
[Collection(E2eCollection.Name)]
public class ApartmentApplicationE2eTests : E2eTestBase
{
    public ApartmentApplicationE2eTests(E2eFixture fixture) : base(fixture) { }

    // ── Apply ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Apply_AsTenant_Returns200WithApplicationId()
    {
        var landlord  = await Data.CreateUserAsync("app-landlord1@e2e.com", role: "Landlord");
        var tenant    = await Data.CreateUserAsync("app-tenant1@e2e.com",   role: "Tenant");
        var apartment = await Data.CreateApartmentAsync(landlord.UserId);
        var client    = CreateAuthenticatedClient(tenant.UserId, tenant.UserGuid);

        var response = await new ApplyForApartmentEndpoint(client).CallAsync(apartment.ApartmentId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("applicationId").GetInt32().Should().BeGreaterThan(0);
        body.GetProperty("status").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Apply_WithoutAuth_Returns401()
    {
        var landlord  = await Data.CreateUserAsync("app-landlord2@e2e.com", role: "Landlord");
        var apartment = await Data.CreateApartmentAsync(landlord.UserId);

        var response = await new ApplyForApartmentEndpoint(HttpClient).CallAsync(apartment.ApartmentId);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Apply_Twice_ReturnsBadRequest()
    {
        var landlord  = await Data.CreateUserAsync("app-landlord3@e2e.com", role: "Landlord");
        var tenant    = await Data.CreateUserAsync("app-tenant3@e2e.com",   role: "Tenant");
        var apartment = await Data.CreateApartmentAsync(landlord.UserId);
        var client    = CreateAuthenticatedClient(tenant.UserId, tenant.UserGuid);

        await new ApplyForApartmentEndpoint(client).CallAsync(apartment.ApartmentId);
        var second = await new ApplyForApartmentEndpoint(client).CallAsync(apartment.ApartmentId);

        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Landlord view ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetLandlordApplications_AsLandlord_ReturnsTenantApplications()
    {
        var landlord  = await Data.CreateUserAsync("app-ll-view@e2e.com", role: "Landlord");
        var tenant    = await Data.CreateUserAsync("app-t-view@e2e.com",  role: "Tenant");
        var apartment = await Data.CreateApartmentAsync(landlord.UserId);

        var tenantClient   = CreateAuthenticatedClient(tenant.UserId, tenant.UserGuid);
        var landlordClient = CreateAuthenticatedClient(landlord.UserId, landlord.UserGuid, "Landlord");

        await new ApplyForApartmentEndpoint(tenantClient).CallAsync(apartment.ApartmentId);

        var response = await new GetLandlordApplicationsEndpoint(landlordClient).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.EnumerateArray().Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetLandlordApplications_AsTenant_ReturnsForbidden()
    {
        var tenant = await Data.CreateUserAsync("app-tenant-forbidden@e2e.com", role: "Tenant");
        var client = CreateAuthenticatedClient(tenant.UserId, tenant.UserGuid);

        var response = await new GetLandlordApplicationsEndpoint(client).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Tenant view ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTenantApplications_ReturnsOwnApplications()
    {
        var landlord  = await Data.CreateUserAsync("app-ll-tv@e2e.com", role: "Landlord");
        var tenantA   = await Data.CreateUserAsync("app-ta-tv@e2e.com", role: "Tenant");
        var tenantB   = await Data.CreateUserAsync("app-tb-tv@e2e.com", role: "Tenant");
        var apartment = await Data.CreateApartmentAsync(landlord.UserId);

        var clientA = CreateAuthenticatedClient(tenantA.UserId, tenantA.UserGuid);
        var clientB = CreateAuthenticatedClient(tenantB.UserId, tenantB.UserGuid);
        await new ApplyForApartmentEndpoint(clientA).CallAsync(apartment.ApartmentId);

        var response = await new GetTenantApplicationsEndpoint(clientA).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<JsonElement>();
        items.EnumerateArray().Should().NotBeEmpty();

        // Tenant B hasn't applied — should see empty list
        var responseB = await new GetTenantApplicationsEndpoint(clientB).CallAsync();
        var itemsB    = await responseB.Content.ReadFromJsonAsync<JsonElement>();
        itemsB.EnumerateArray().Should().BeEmpty();
    }

    // ── Full flow: apply → approve → check approval ───────────────────────────

    [Fact]
    public async Task FullFlow_TenantApplies_LandlordApproves_TenantSeesApproved()
    {
        var landlord  = await Data.CreateUserAsync("app-ll-full@e2e.com", role: "Landlord");
        var tenant    = await Data.CreateUserAsync("app-t-full@e2e.com",  role: "Tenant");
        var apartment = await Data.CreateApartmentAsync(landlord.UserId);

        var tenantClient   = CreateAuthenticatedClient(tenant.UserId, tenant.UserGuid);
        var landlordClient = CreateAuthenticatedClient(landlord.UserId, landlord.UserGuid, "Landlord");

        // 1. Tenant applies
        var applyResp = await new ApplyForApartmentEndpoint(tenantClient).CallAsync(apartment.ApartmentId);
        applyResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var applyBody    = await applyResp.Content.ReadFromJsonAsync<JsonElement>();
        var applicationId = applyBody.GetProperty("applicationId").GetInt32();

        // 2. Landlord approves
        var approveResp = await new UpdateApplicationStatusEndpoint(landlordClient)
            .CallAsync(applicationId, "Approved");
        approveResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var approveBody = await approveResp.Content.ReadFromJsonAsync<JsonElement>();
        approveBody.GetProperty("status").GetString().Should().Be("Approved");

        // 3. Tenant checks approval status
        var checkResp = await new CheckApprovalEndpoint(tenantClient).CallAsync(apartment.ApartmentId);
        checkResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var checkBody = await checkResp.Content.ReadFromJsonAsync<JsonElement>();
        checkBody.GetProperty("hasApprovedApplication").GetBoolean().Should().BeTrue();
        checkBody.GetProperty("applicationStatus").GetString().Should().Be("Approved");
    }

    [Fact]
    public async Task UpdateStatus_ByNonOwnerLandlord_ReturnsForbidOrNotFound()
    {
        var ownerLandlord   = await Data.CreateUserAsync("app-owner-ll@e2e.com",    role: "Landlord");
        var intruderLandlord = await Data.CreateUserAsync("app-intruder-ll@e2e.com", role: "Landlord");
        var tenant           = await Data.CreateUserAsync("app-tenant-sec@e2e.com",  role: "Tenant");
        var apartment        = await Data.CreateApartmentAsync(ownerLandlord.UserId);

        var tenantClient    = CreateAuthenticatedClient(tenant.UserId, tenant.UserGuid);
        var intruderClient  = CreateAuthenticatedClient(intruderLandlord.UserId, intruderLandlord.UserGuid, "Landlord");

        var applyResp = await new ApplyForApartmentEndpoint(tenantClient).CallAsync(apartment.ApartmentId);
        var appId     = (await applyResp.Content.ReadFromJsonAsync<JsonElement>())
                            .GetProperty("applicationId").GetInt32();

        var response = await new UpdateApplicationStatusEndpoint(intruderClient)
            .CallAsync(appId, "Approved");

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.NotFound,
            HttpStatusCode.BadRequest);
    }
}
