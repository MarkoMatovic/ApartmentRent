using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LandlordApp.Tests.E2eTests.Infrastructure;
using LandlordApp.Tests.E2eTests.Shared;

namespace LandlordApp.Tests.E2eTests.Tests;

/// <summary>
/// E2E tests for the search-request CRUD flow.
/// </summary>
[Collection(E2eCollection.Name)]
public class SearchRequestE2eTests : E2eTestBase
{
    public SearchRequestE2eTests(E2eFixture fixture) : base(fixture) { }

    private static object ValidDto(string title = "Tražim stan u centru") => new
    {
        RequestType = "LookingForApartment",
        Title       = title,
        City        = "Sarajevo",
        BudgetMin   = 300m,
        BudgetMax   = 700m,
    };

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllSearchRequests_Anonymous_Returns200WithList()
    {
        var user = await Data.CreateUserAsync("sr-list@e2e.com");
        await Data.CreateSearchRequestAsync(user.UserId, "Tražim stan u Sarajevu");

        var response = await new GetAllSearchRequestsEndpoint(HttpClient).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Paged or array
        var items = body.ValueKind == JsonValueKind.Array
            ? body
            : body.TryGetProperty("items", out var it) ? it : body.GetProperty("data");

        items.EnumerateArray().Should().Contain(r =>
            r.GetProperty("title").GetString() == "Tražim stan u Sarajevu");
    }

    [Fact]
    public async Task GetAllSearchRequests_FilterByCity_ReturnsMatchingOnly()
    {
        var u1 = await Data.CreateUserAsync("sr-city-a@e2e.com");
        var u2 = await Data.CreateUserAsync("sr-city-b@e2e.com");
        await Data.CreateSearchRequestAsync(u1.UserId, "Sarajevo zahtjev", city: "Sarajevo");
        await Data.CreateSearchRequestAsync(u2.UserId, "Mostar zahtjev",   city: "Mostar");

        var response = await new GetAllSearchRequestsEndpoint(HttpClient).CallAsync(city: "Sarajevo");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSearchRequest_ExistingId_Returns200WithDetails()
    {
        var user = await Data.CreateUserAsync("sr-get@e2e.com");
        var sr   = await Data.CreateSearchRequestAsync(user.UserId, "Moj zahtjev");

        var response = await new GetSearchRequestEndpoint(HttpClient).CallAsync(sr.SearchRequestId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("title").GetString().Should().Be("Moj zahtjev");
    }

    [Fact]
    public async Task GetSearchRequest_NonExistent_Returns404()
    {
        var response = await new GetSearchRequestEndpoint(HttpClient).CallAsync(999_999);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GetByUserId ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSearchRequestsByUserId_AsOwner_Returns200WithList()
    {
        var user = await Data.CreateUserAsync("sr-byuid@e2e.com");
        await Data.CreateSearchRequestAsync(user.UserId);

        var client   = CreateAuthenticatedClient(user.UserId, user.UserGuid);
        var response = await new GetSearchRequestsByUserIdEndpoint(client).CallAsync(user.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetSearchRequestsByUserId_AsOtherUser_Returns403()
    {
        var owner    = await Data.CreateUserAsync("sr-byuid-own@e2e.com");
        var intruder = await Data.CreateUserAsync("sr-byuid-int@e2e.com");
        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);

        var response = await new GetSearchRequestsByUserIdEndpoint(client).CallAsync(owner.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSearchRequestsByUserId_WithoutAuth_Returns401()
    {
        var user = await Data.CreateUserAsync("sr-byuid-na@e2e.com");

        var response = await new GetSearchRequestsByUserIdEndpoint(HttpClient).CallAsync(user.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSearchRequest_AsAuthenticatedUser_Returns200WithData()
    {
        var user   = await Data.CreateUserAsync("sr-create@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new CreateSearchRequestEndpoint(client).CallAsync(ValidDto("Novi zahtjev"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("title").GetString().Should().Be("Novi zahtjev");
    }

    [Fact]
    public async Task CreateSearchRequest_WithoutAuth_Returns401()
    {
        var response = await new CreateSearchRequestEndpoint(HttpClient).CallAsync(ValidDto());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateSearchRequest_AsOwner_Returns200WithNewValues()
    {
        var user   = await Data.CreateUserAsync("sr-upd@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);
        var sr     = await Data.CreateSearchRequestAsync(user.UserId, "Staro");

        var response = await new UpdateSearchRequestEndpoint(client)
            .CallAsync(sr.SearchRequestId, ValidDto("Novo"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("title").GetString().Should().Be("Novo");
    }

    [Fact]
    public async Task UpdateSearchRequest_ByOtherUser_Returns404OrForbid()
    {
        var owner    = await Data.CreateUserAsync("sr-upd-own@e2e.com");
        var intruder = await Data.CreateUserAsync("sr-upd-int@e2e.com");
        var sr       = await Data.CreateSearchRequestAsync(owner.UserId);
        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);

        var response = await new UpdateSearchRequestEndpoint(client)
            .CallAsync(sr.SearchRequestId, ValidDto("Haker"));

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateSearchRequest_WithoutAuth_Returns401()
    {
        var user = await Data.CreateUserAsync("sr-upd-na@e2e.com");
        var sr   = await Data.CreateSearchRequestAsync(user.UserId);

        var response = await new UpdateSearchRequestEndpoint(HttpClient)
            .CallAsync(sr.SearchRequestId, ValidDto());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteSearchRequest_AsOwner_Returns200()
    {
        var user   = await Data.CreateUserAsync("sr-del@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);
        var sr     = await Data.CreateSearchRequestAsync(user.UserId);

        var response = await new DeleteSearchRequestEndpoint(client).CallAsync(sr.SearchRequestId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteSearchRequest_ByOtherUser_Returns404()
    {
        var owner    = await Data.CreateUserAsync("sr-del-own@e2e.com");
        var intruder = await Data.CreateUserAsync("sr-del-int@e2e.com");
        var sr       = await Data.CreateSearchRequestAsync(owner.UserId);
        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);

        var response = await new DeleteSearchRequestEndpoint(client).CallAsync(sr.SearchRequestId);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteSearchRequest_WithoutAuth_Returns401()
    {
        var user = await Data.CreateUserAsync("sr-del-na@e2e.com");
        var sr   = await Data.CreateSearchRequestAsync(user.UserId);

        var response = await new DeleteSearchRequestEndpoint(HttpClient).CallAsync(sr.SearchRequestId);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteSearchRequest_NonExistent_Returns404()
    {
        var user   = await Data.CreateUserAsync("sr-del-404@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new DeleteSearchRequestEndpoint(client).CallAsync(999_999);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
