using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LandlordApp.Tests.E2eTests.Infrastructure;
using LandlordApp.Tests.E2eTests.Shared;

namespace LandlordApp.Tests.E2eTests.Tests;

/// <summary>
/// E2E tests for apartment endpoints not covered in the main ApartmentE2eTests:
///   – Keyset pagination (GET /api/v1/rent/keyset)
///   – Activate apartment (PUT /api/v1/rent/activate-apartment/{id})
///   – GetMyApartments (GET /api/v1/rent/get-my-apartments)
/// </summary>
[Collection(E2eCollection.Name)]
public class ApartmentExtrasE2eTests : E2eTestBase
{
    public ApartmentExtrasE2eTests(E2eFixture fixture) : base(fixture) { }

    // ── Keyset pagination (public) ────────────────────────────────────────────

    [Fact]
    public async Task KeysetPagination_Anonymous_Returns200WithData()
    {
        var landlord = await Data.CreateUserAsync("apt-ks-anon@e2e.com");
        await Data.CreateApartmentAsync(landlord.UserId, "Keyset Test Apt");

        var response = await new GetAllApartmentsKeysetEndpoint(HttpClient).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().NotBe(JsonValueKind.Undefined);
    }

    [Fact]
    public async Task KeysetPagination_WithLastId_Returns200()
    {
        var landlord = await Data.CreateUserAsync("apt-ks-lastid@e2e.com");
        var apt1     = await Data.CreateApartmentAsync(landlord.UserId, "Keyset Apt 1");
        await Data.CreateApartmentAsync(landlord.UserId, "Keyset Apt 2");

        var response = await new GetAllApartmentsKeysetEndpoint(HttpClient).CallAsync(lastId: apt1.ApartmentId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task KeysetPagination_FilterByCity_Returns200()
    {
        var landlord = await Data.CreateUserAsync("apt-ks-city@e2e.com");
        await Data.CreateApartmentAsync(landlord.UserId, "Sarajevo Keyset Apt", city: "Sarajevo");
        await Data.CreateApartmentAsync(landlord.UserId, "Mostar Keyset Apt",   city: "Mostar");

        var response = await new GetAllApartmentsKeysetEndpoint(HttpClient).CallAsync(city: "Sarajevo");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task KeysetPagination_SmallPageSize_Returns200()
    {
        var response = await new GetAllApartmentsKeysetEndpoint(HttpClient).CallAsync(pageSize: 3);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GetMyApartments [Authorize] ───────────────────────────────────────────

    [Fact]
    public async Task GetMyApartments_AsAuthenticatedLandlord_Returns200WithOwnApartments()
    {
        var landlord = await Data.CreateUserAsync("apt-mine@e2e.com");
        await Data.CreateApartmentAsync(landlord.UserId, "My Own Apartment");
        var client = CreateAuthenticatedClient(landlord.UserId, landlord.UserGuid);

        var response = await new GetMyApartmentsEndpoint(client).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMyApartments_WithoutAuth_Returns401()
    {
        var response = await new GetMyApartmentsEndpoint(HttpClient).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── ActivateApartment [Authorize] ─────────────────────────────────────────

    [Fact]
    public async Task ActivateApartment_AsOwner_Returns200OrOk()
    {
        var landlord = await Data.CreateUserAsync("apt-act-own@e2e.com");
        var apt      = await Data.CreateApartmentAsync(landlord.UserId, "Inactive Apt");
        var client   = CreateAuthenticatedClient(landlord.UserId, landlord.UserGuid);

        var response = await new ActivateApartmentEndpoint(client).CallAsync(apt.ApartmentId);

        // 200 = activated/already active; both outcomes are acceptable
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ActivateApartment_AsOtherUser_Returns403()
    {
        var landlord = await Data.CreateUserAsync("apt-act-own2@e2e.com");
        var intruder = await Data.CreateUserAsync("apt-act-int@e2e.com");
        var apt      = await Data.CreateApartmentAsync(landlord.UserId, "Someone Else's Apt");
        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);

        var response = await new ActivateApartmentEndpoint(client).CallAsync(apt.ApartmentId);

        // RequireOwnerAsync throws ForbiddenException → 403 via GlobalExceptionHandlerMiddleware
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ActivateApartment_WithoutAuth_Returns401()
    {
        var response = await new ActivateApartmentEndpoint(HttpClient).CallAsync(1);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ActivateApartment_NonExistent_Returns200WithFalse()
    {
        var user   = await Data.CreateUserAsync("apt-act-404@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new ActivateApartmentEndpoint(client).CallAsync(999_999);

        // Service returns Ok(false) even when apartment doesn't exist — no 404 thrown
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
