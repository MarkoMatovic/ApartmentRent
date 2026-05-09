using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LandlordApp.Tests.E2eTests.Infrastructure;
using LandlordApp.Tests.E2eTests.Shared;

namespace LandlordApp.Tests.E2eTests.Tests;

/// <summary>
/// E2E tests for the apartment CRUD flow.
/// All tests run against a real SQL Server container with a clean DB.
/// </summary>
[Collection(E2eCollection.Name)]
public class ApartmentE2eTests : E2eTestBase
{
    public ApartmentE2eTests(E2eFixture fixture) : base(fixture) { }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateApartment_AsLandlord_Returns200WithId()
    {
        var landlord = await Data.CreateUserAsync("landlord-create@e2e.com", role: "Landlord");
        var client   = CreateAuthenticatedClient(landlord.UserId, landlord.UserGuid, "Landlord");

        var response = await new CreateApartmentEndpoint(client).CallAsync(new
        {
            Title       = "Lijepi stan u centru",
            Description = "Prostran stan u strogom centru sa svim sadrzajima.",
            Rent        = 600m,
            Address     = "Ferhadija 1",
            City        = "Sarajevo",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("apartmentId").GetInt32().Should().BeGreaterThan(0);
        body.GetProperty("title").GetString().Should().Be("Lijepi stan u centru");
    }

    [Fact]
    public async Task CreateApartment_WithoutAuth_Returns401()
    {
        var response = await new CreateApartmentEndpoint(HttpClient).CallAsync(new
        {
            Title   = "Stan bez auth",
            Rent    = 400m,
            Address = "Negdje 5",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Read ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllApartments_ReturnsSeededApartment()
    {
        var landlord = await Data.CreateUserAsync("landlord-list@e2e.com", role: "Landlord");
        await Data.CreateApartmentAsync(landlord.UserId, title: "Stan za listu", rent: 550m);

        var response = await new GetAllApartmentsEndpoint(HttpClient).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Response is either array or paged object { items: [...] }
        var items = body.ValueKind == JsonValueKind.Array
            ? body
            : body.GetProperty("items");

        items.EnumerateArray()
             .Should().Contain(a =>
                 a.GetProperty("title").GetString() == "Stan za listu");
    }

    [Fact]
    public async Task GetAllApartments_FilterByCity_ReturnsOnlyMatchingCity()
    {
        var landlord = await Data.CreateUserAsync("landlord-city@e2e.com", role: "Landlord");
        await Data.CreateApartmentAsync(landlord.UserId, title: "Mostar stan", rent: 400m, city: "Mostar");

        var response = await new GetAllApartmentsEndpoint(HttpClient).CallAsync(city: "Mostar");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body  = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = body.ValueKind == JsonValueKind.Array
            ? body
            : body.GetProperty("items");

        items.EnumerateArray()
             .Should().OnlyContain(a =>
                 a.GetProperty("city").GetString() == "Mostar");
    }

    [Fact]
    public async Task GetApartment_ExistingId_Returns200WithDetails()
    {
        var landlord   = await Data.CreateUserAsync("landlord-get@e2e.com", role: "Landlord");
        var apartment  = await Data.CreateApartmentAsync(landlord.UserId, title: "Detalj stan", rent: 700m);

        var response = await new GetApartmentEndpoint(HttpClient)
            .CallAsync(apartment.ApartmentId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("title").GetString().Should().Be("Detalj stan");
    }

    [Fact]
    public async Task GetApartment_NonExistentId_Returns404()
    {
        var response = await new GetApartmentEndpoint(HttpClient).CallAsync(999_999);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetApartment_Unauthenticated_HidesLandlordEmail()
    {
        var landlord  = await Data.CreateUserAsync("landlord-email@e2e.com", role: "Landlord");
        var apartment = await Data.CreateApartmentAsync(landlord.UserId);

        var response = await new GetApartmentEndpoint(HttpClient)
            .CallAsync(apartment.ApartmentId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        if (body.TryGetProperty("landlordEmail", out var emailProp))
            emailProp.GetString().Should().BeNullOrEmpty();
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateApartment_ByOwner_Returns200WithNewValues()
    {
        var landlord  = await Data.CreateUserAsync("landlord-upd@e2e.com", role: "Landlord");
        var client    = CreateAuthenticatedClient(landlord.UserId, landlord.UserGuid, "Landlord");
        var apartment = await Data.CreateApartmentAsync(landlord.UserId, title: "Stari naziv");

        var response = await new UpdateApartmentEndpoint(client)
            .CallAsync(apartment.ApartmentId, new { Title = "Novi naziv", Rent = 800m });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("title").GetString().Should().Be("Novi naziv");
        body.GetProperty("rent").GetDecimal().Should().Be(800m);
    }

    [Fact]
    public async Task UpdateApartment_ByOtherLandlord_ReturnsForbidOrNotFound()
    {
        var owner     = await Data.CreateUserAsync("owner@e2e.com", role: "Landlord");
        var intruder  = await Data.CreateUserAsync("intruder@e2e.com", role: "Landlord");
        var apartment = await Data.CreateApartmentAsync(owner.UserId);
        var client    = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid, "Landlord");

        var response = await new UpdateApartmentEndpoint(client)
            .CallAsync(apartment.ApartmentId, new { Title = "Hakovan" });

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden,
            HttpStatusCode.Unauthorized,
            HttpStatusCode.NotFound,
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateApartment_WithoutAuth_Returns401()
    {
        var landlord  = await Data.CreateUserAsync("landlord-upd2@e2e.com", role: "Landlord");
        var apartment = await Data.CreateApartmentAsync(landlord.UserId);

        var response = await new UpdateApartmentEndpoint(HttpClient)
            .CallAsync(apartment.ApartmentId, new { Title = "Ne bi trebalo" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteApartment_ByOwner_Returns200AndApartmentGone()
    {
        var landlord  = await Data.CreateUserAsync("landlord-del@e2e.com", role: "Landlord");
        var client    = CreateAuthenticatedClient(landlord.UserId, landlord.UserGuid, "Landlord");
        var apartment = await Data.CreateApartmentAsync(landlord.UserId, title: "Stan za brisanje");

        var deleteResp = await new DeleteApartmentEndpoint(client).CallAsync(apartment.ApartmentId);
        deleteResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // After delete the apartment should no longer be visible (soft-deleted / inactive)
        var getResp = await new GetApartmentEndpoint(HttpClient).CallAsync(apartment.ApartmentId);
        getResp.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Gone);
    }

    [Fact]
    public async Task DeleteApartment_WithoutAuth_Returns401()
    {
        var landlord  = await Data.CreateUserAsync("landlord-del2@e2e.com", role: "Landlord");
        var apartment = await Data.CreateApartmentAsync(landlord.UserId);

        var response = await new DeleteApartmentEndpoint(HttpClient).CallAsync(apartment.ApartmentId);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── My apartments ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyApartments_ReturnsOnlyOwnApartments()
    {
        var landlordA = await Data.CreateUserAsync("mine-a@e2e.com", role: "Landlord");
        var landlordB = await Data.CreateUserAsync("mine-b@e2e.com", role: "Landlord");
        await Data.CreateApartmentAsync(landlordA.UserId, title: "Moj stan A");
        await Data.CreateApartmentAsync(landlordB.UserId, title: "Tuđi stan B");

        var clientA = CreateAuthenticatedClient(landlordA.UserId, landlordA.UserGuid, "Landlord");
        var response = await clientA.GetAsync("/api/v1/rent/get-my-apartments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body  = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = body.ValueKind == JsonValueKind.Array
            ? body
            : body.GetProperty("items");

        var titles = items.EnumerateArray()
                          .Select(a => a.GetProperty("title").GetString())
                          .ToList();

        titles.Should().Contain("Moj stan A");
        titles.Should().NotContain("Tuđi stan B");
    }
}
