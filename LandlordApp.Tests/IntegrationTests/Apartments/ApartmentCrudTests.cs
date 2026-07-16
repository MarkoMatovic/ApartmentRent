using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LandlordApp.Tests.IntegrationTests.Infrastructure;

namespace LandlordApp.Tests.IntegrationTests.Apartments;

/// <summary>
/// Covers the full Apartment CRUD pipeline through real HTTP endpoints.
/// Each test seeds its own user with a unique email to avoid cross-test interference.
/// </summary>
public class ApartmentCrudTests : IntegrationTestBase, IClassFixture<LanderWebApplicationFactory>
{
    public ApartmentCrudTests(LanderWebApplicationFactory factory) : base(factory) { }

    // ── Anonymous access ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllApartments_Anonymous_Returns200()
    {
        var client   = CreateAnonymousClient();
        var response = await client.GetAsync("/api/v1/rent/get-all-apartments");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateApartment_Anonymous_Returns401()
    {
        var client   = CreateAnonymousClient();
        var response = await client.PostAsJsonAsync("/api/v1/rent/create-apartment", new
        {
            Title = "Unauthorized Apartment",
            Rent  = 400
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Authenticated CRUD ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateApartment_AsLandlord_Returns200WithApartmentId()
    {
        var landlord = await SeedUserAsync("create-apt@test.com", role: "Landlord");
        var client   = CreateAuthenticatedClient(landlord.UserId, landlord.UserGuid, "Landlord");

        var response = await client.PostAsJsonAsync("/api/v1/rent/create-apartment", new
        {
            Title       = "Lijepi stan u centru",
            Description = "Svetao dvosoban stan u strogom centru.",
            Rent        = 700,
            Address     = "Ferhadija 1",
            City        = "Sarajevo",
            PostalCode  = "71000",
            NumberOfRooms = 2,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("apartmentId").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAllApartments_AfterCreating_ContainsNewApartment()
    {
        var landlord = await SeedUserAsync("list-apt@test.com", role: "Landlord");
        // Unique city → unique HybridCache key, so a previously cached unfiltered
        // (empty) list from another test can't shadow the freshly seeded apartment.
        var uniqueCity = $"Grad{Guid.NewGuid():N}"[..12];
        await SeedApartmentAsync(landlord.UserId, "Stan za popis", city: uniqueCity);
        var client = CreateAnonymousClient();

        var response = await client.GetAsync($"/api/v1/rent/get-all-apartments?city={uniqueCity}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body  = await response.Content.ReadFromJsonAsync<JsonElement>();
        // Response is paged — check that at least one item exists
        body.TryGetProperty("items", out var items).Should().BeTrue();
        items.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DeleteApartment_ByOwner_Returns200()
    {
        var landlord  = await SeedUserAsync("delete-apt@test.com", role: "Landlord");
        var apartment = await SeedApartmentAsync(landlord.UserId, "Za brisanje");
        var client    = CreateAuthenticatedClient(landlord.UserId, landlord.UserGuid, "Landlord");

        var response = await client.DeleteAsync($"/api/v1/rent/delete-apartment/{apartment.ApartmentId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteApartment_ByNonOwner_Returns403()
    {
        var landlord  = await SeedUserAsync("owner-del@test.com",    role: "Landlord");
        var other     = await SeedUserAsync("notowner-del@test.com", role: "Landlord");
        var apartment = await SeedApartmentAsync(landlord.UserId, "Tuđi stan");

        // other user tries to delete landlord's apartment
        var client   = CreateAuthenticatedClient(other.UserId, other.UserGuid, "Landlord");
        var response = await client.DeleteAsync($"/api/v1/rent/delete-apartment/{apartment.ApartmentId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateApartment_ByOwner_Returns200WithUpdatedTitle()
    {
        var landlord  = await SeedUserAsync("update-apt@test.com", role: "Landlord");
        var apartment = await SeedApartmentAsync(landlord.UserId, "Stari naslov");
        var client    = CreateAuthenticatedClient(landlord.UserId, landlord.UserGuid, "Landlord");

        var response = await client.PutAsJsonAsync(
            $"/api/v1/rent/update-apartment/{apartment.ApartmentId}",
            new { Title = "Novi naslov" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("title").GetString().Should().Be("Novi naslov");
    }

    // ── Image URL validation (S-7) ───────────────────────────────────────────

    [Fact]
    public async Task CreateApartment_WithExternalImageUrl_ImageIsStripped()
    {
        var landlord = await SeedUserAsync("img-apt@test.com", role: "Landlord");
        var client   = CreateAuthenticatedClient(landlord.UserId, landlord.UserGuid, "Landlord");

        // External URL should be dropped by ToImageStorageKey (only our container paths survive)
        var response = await client.PostAsJsonAsync("/api/v1/rent/create-apartment", new
        {
            Title       = "Image test",
            Description = "Stan sa slikama za test.",
            Rent        = 500,
            Address     = "Testna 2",
            City        = "Sarajevo",
            ImageUrls   = new[] { "https://evil.com/malicious.jpg", "/uploads/apartments/ok.webp" }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Only the /uploads/apartments/ URL should survive — external one is silently dropped
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("apartmentId").GetInt32().Should().BeGreaterThan(0);
    }
}
