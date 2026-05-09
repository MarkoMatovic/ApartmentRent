using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LandlordApp.Tests.E2eTests.Infrastructure;
using LandlordApp.Tests.E2eTests.Shared;

namespace LandlordApp.Tests.E2eTests.Tests;

/// <summary>
/// E2E tests for the saved-search CRUD flow.
/// </summary>
[Collection(E2eCollection.Name)]
public class SavedSearchE2eTests : E2eTestBase
{
    public SavedSearchE2eTests(E2eFixture fixture) : base(fixture) { }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSavedSearch_AsAuthenticatedUser_Returns200WithData()
    {
        var user   = await Data.CreateUserAsync("ss-create@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new CreateSavedSearchEndpoint(client).CallAsync(new
        {
            Name                     = "Moja pretraga",
            SearchType               = "Apartments",
            FiltersJson              = "{\"city\":\"Sarajevo\"}",
            EmailNotificationsEnabled = true,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("name").GetString().Should().Be("Moja pretraga");
    }

    [Fact]
    public async Task CreateSavedSearch_WithoutAuth_Returns401()
    {
        var response = await new CreateSavedSearchEndpoint(HttpClient).CallAsync(new
        {
            Name       = "Bez autentikacije",
            SearchType = "Apartments",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Read ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSavedSearchesByUserId_AsOwner_Returns200WithList()
    {
        var user   = await Data.CreateUserAsync("ss-list@e2e.com");
        await Data.CreateSavedSearchAsync(user.UserId, name: "Pretraga 1");
        await Data.CreateSavedSearchAsync(user.UserId, name: "Pretraga 2");

        var client   = CreateAuthenticatedClient(user.UserId, user.UserGuid);
        var response = await new GetSavedSearchesByUserIdEndpoint(client).CallAsync(user.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
        body.GetArrayLength().Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetSavedSearchesByUserId_AsOtherUser_Returns403()
    {
        var owner    = await Data.CreateUserAsync("ss-list-own@e2e.com");
        var intruder = await Data.CreateUserAsync("ss-list-int@e2e.com");
        await Data.CreateSavedSearchAsync(owner.UserId);

        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);
        var response = await new GetSavedSearchesByUserIdEndpoint(client).CallAsync(owner.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSavedSearchesByUserId_WithoutAuth_Returns401()
    {
        var user = await Data.CreateUserAsync("ss-list-na@e2e.com");

        var response = await new GetSavedSearchesByUserIdEndpoint(HttpClient).CallAsync(user.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSavedSearch_AsOwner_Returns200WithDetails()
    {
        var user   = await Data.CreateUserAsync("ss-get@e2e.com");
        var search = await Data.CreateSavedSearchAsync(user.UserId, name: "Tražena pretraga");

        var client   = CreateAuthenticatedClient(user.UserId, user.UserGuid);
        var response = await new GetSavedSearchEndpoint(client).CallAsync(search.SavedSearchId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("name").GetString().Should().Be("Tražena pretraga");
    }

    [Fact]
    public async Task GetSavedSearch_AsOtherUser_Returns403()
    {
        var owner    = await Data.CreateUserAsync("ss-get-own@e2e.com");
        var intruder = await Data.CreateUserAsync("ss-get-int@e2e.com");
        var search   = await Data.CreateSavedSearchAsync(owner.UserId);

        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);
        var response = await new GetSavedSearchEndpoint(client).CallAsync(search.SavedSearchId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSavedSearch_NonExistent_Returns404()
    {
        var user   = await Data.CreateUserAsync("ss-get-404@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new GetSavedSearchEndpoint(client).CallAsync(999_999);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSavedSearch_WithoutAuth_Returns401()
    {
        var user   = await Data.CreateUserAsync("ss-get-na@e2e.com");
        var search = await Data.CreateSavedSearchAsync(user.UserId);

        var response = await new GetSavedSearchEndpoint(HttpClient).CallAsync(search.SavedSearchId);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateSavedSearch_AsOwner_Returns200WithNewValues()
    {
        var user   = await Data.CreateUserAsync("ss-upd@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);
        var search = await Data.CreateSavedSearchAsync(user.UserId, name: "Staro ime");

        var response = await new UpdateSavedSearchEndpoint(client).CallAsync(search.SavedSearchId, new
        {
            Name                     = "Novo ime",
            SearchType               = "Apartments",
            FiltersJson              = "{\"city\":\"Mostar\"}",
            EmailNotificationsEnabled = false,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("name").GetString().Should().Be("Novo ime");
    }

    [Fact]
    public async Task UpdateSavedSearch_ByOtherUser_ReturnsForbidOrNotFound()
    {
        var owner    = await Data.CreateUserAsync("ss-upd-own@e2e.com");
        var intruder = await Data.CreateUserAsync("ss-upd-int@e2e.com");
        var search   = await Data.CreateSavedSearchAsync(owner.UserId);
        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);

        var response = await new UpdateSavedSearchEndpoint(client).CallAsync(search.SavedSearchId, new
        {
            Name       = "Pokušaj hakera",
            SearchType = "Apartments",
        });

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateSavedSearch_WithoutAuth_Returns401()
    {
        var user   = await Data.CreateUserAsync("ss-upd-na@e2e.com");
        var search = await Data.CreateSavedSearchAsync(user.UserId);

        var response = await new UpdateSavedSearchEndpoint(HttpClient).CallAsync(search.SavedSearchId, new
        {
            Name       = "Bez autentikacije",
            SearchType = "Apartments",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteSavedSearch_AsOwner_Returns200()
    {
        var user   = await Data.CreateUserAsync("ss-del@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);
        var search = await Data.CreateSavedSearchAsync(user.UserId);

        var response = await new DeleteSavedSearchEndpoint(client).CallAsync(search.SavedSearchId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteSavedSearch_ByOtherUser_Returns404()
    {
        var owner    = await Data.CreateUserAsync("ss-del-own@e2e.com");
        var intruder = await Data.CreateUserAsync("ss-del-int@e2e.com");
        var search   = await Data.CreateSavedSearchAsync(owner.UserId);
        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);

        var response = await new DeleteSavedSearchEndpoint(client).CallAsync(search.SavedSearchId);

        // Service returns false (not found for this user) → controller returns NotFound
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound,
            HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteSavedSearch_WithoutAuth_Returns401()
    {
        var user   = await Data.CreateUserAsync("ss-del-na@e2e.com");
        var search = await Data.CreateSavedSearchAsync(user.UserId);

        var response = await new DeleteSavedSearchEndpoint(HttpClient).CallAsync(search.SavedSearchId);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteSavedSearch_NonExistent_Returns404()
    {
        var user   = await Data.CreateUserAsync("ss-del-404@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new DeleteSavedSearchEndpoint(client).CallAsync(999_999);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
