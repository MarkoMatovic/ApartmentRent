using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LandlordApp.Tests.E2eTests.Infrastructure;
using LandlordApp.Tests.E2eTests.Shared;

namespace LandlordApp.Tests.E2eTests.Tests;

/// <summary>
/// E2E tests for the roommate profile CRUD flow.
/// </summary>
[Collection(E2eCollection.Name)]
public class RoommateE2eTests : E2eTestBase
{
    public RoommateE2eTests(E2eFixture fixture) : base(fixture) { }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateRoommate_AsAuthenticatedUser_Returns200WithId()
    {
        var user   = await Data.CreateUserAsync("rm-create@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new CreateRoommateEndpoint(client).CallAsync(new
        {
            Bio             = "Tražim cimera u Sarajevu.",
            Profession      = "Inženjer",
            BudgetMin       = 300m,
            BudgetMax       = 600m,
            PreferredLocation = "Sarajevo",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("roommateId").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateRoommate_WithoutAuth_Returns401()
    {
        var response = await new CreateRoommateEndpoint(HttpClient).CallAsync(new
        {
            Profession = "Dizajner",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Read ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllRoommates_ReturnsSeededProfile()
    {
        // Unique location → unique service-cache key; a directly seeded roommate
        // does not bump the list-cache version, so the default list may be stale.
        var user = await Data.CreateUserAsync("rm-list@e2e.com");
        await Data.CreateRoommateAsync(user.UserId, profession: "Arhitekt", location: "Arhitektgrad");

        var response = await new GetAllRoommatesEndpoint(HttpClient).CallAsync(location: "Arhitektgrad");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Response is either array or paged { items: [...] }
        var items = body.ValueKind == JsonValueKind.Array
            ? body
            : body.GetProperty("items");

        items.EnumerateArray()
             .Should().Contain(r =>
                 r.GetProperty("profession").GetString() == "Arhitekt");
    }

    [Fact]
    public async Task GetAllRoommates_FilterByLocation_ReturnsMatchingOnly()
    {
        var userA = await Data.CreateUserAsync("rm-loc-a@e2e.com");
        var userB = await Data.CreateUserAsync("rm-loc-b@e2e.com");
        await Data.CreateRoommateAsync(userA.UserId, location: "Mostar");
        await Data.CreateRoommateAsync(userB.UserId, location: "Banja Luka");

        var response = await new GetAllRoommatesEndpoint(HttpClient).CallAsync(location: "Mostar");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body  = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = body.ValueKind == JsonValueKind.Array ? body : body.GetProperty("items");

        items.EnumerateArray()
             .Should().OnlyContain(r =>
                 r.GetProperty("preferredLocation").GetString() == "Mostar");
    }

    [Fact]
    public async Task GetRoommate_ExistingId_Returns200WithDetails()
    {
        var user     = await Data.CreateUserAsync("rm-get@e2e.com");
        var roommate = await Data.CreateRoommateAsync(user.UserId, profession: "Doktor");

        var response = await new GetRoommateEndpoint(HttpClient).CallAsync(roommate.RoommateId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("profession").GetString().Should().Be("Doktor");
    }

    [Fact]
    public async Task GetRoommate_NonExistent_Returns404()
    {
        var response = await new GetRoommateEndpoint(HttpClient).CallAsync(999_999);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRoommate_Unauthenticated_HidesPhoneAndDob()
    {
        var user     = await Data.CreateUserAsync("rm-pii@e2e.com");
        var roommate = await Data.CreateRoommateAsync(user.UserId);

        // Anonymous request → PII should be stripped
        var response = await new GetRoommateEndpoint(HttpClient).CallAsync(roommate.RoommateId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        if (body.TryGetProperty("phoneNumber", out var phone))
            phone.GetString().Should().BeNullOrEmpty();

        if (body.TryGetProperty("dateOfBirth", out var dob))
            dob.ValueKind.Should().BeOneOf(JsonValueKind.Null, JsonValueKind.Undefined);
    }

    [Fact]
    public async Task GetRoommateByUserId_ExistingUser_Returns200()
    {
        var user     = await Data.CreateUserAsync("rm-byuid@e2e.com");
        var roommate = await Data.CreateRoommateAsync(user.UserId, profession: "Pravnik");

        var response = await new GetRoommateByUserIdEndpoint(HttpClient).CallAsync(user.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("profession").GetString().Should().Be("Pravnik");
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateRoommate_ByOwner_Returns200WithNewValues()
    {
        var user     = await Data.CreateUserAsync("rm-upd@e2e.com");
        var client   = CreateAuthenticatedClient(user.UserId, user.UserGuid);
        var roommate = await Data.CreateRoommateAsync(user.UserId, profession: "Stari posao");

        var response = await new UpdateRoommateEndpoint(client)
            .CallAsync(roommate.RoommateId, new { Bio = "Ažurirani profil.", Profession = "Novi posao" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("profession").GetString().Should().Be("Novi posao");
    }

    [Fact]
    public async Task UpdateRoommate_ByOtherUser_ReturnsForbidOrNotFound()
    {
        var owner    = await Data.CreateUserAsync("rm-upd-owner@e2e.com");
        var intruder = await Data.CreateUserAsync("rm-upd-int@e2e.com");
        var roommate = await Data.CreateRoommateAsync(owner.UserId, profession: "Vlasnik");
        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);

        var response = await new UpdateRoommateEndpoint(client)
            .CallAsync(roommate.RoommateId, new { Bio = "Hakerov profil.", Profession = "Haker" });

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound,
            HttpStatusCode.Unauthorized);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteRoommate_ByOwner_Returns200()
    {
        var user     = await Data.CreateUserAsync("rm-del@e2e.com");
        var client   = CreateAuthenticatedClient(user.UserId, user.UserGuid);
        var roommate = await Data.CreateRoommateAsync(user.UserId);

        var response = await new DeleteRoommateEndpoint(client).CallAsync(roommate.RoommateId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteRoommate_ByOtherUser_ReturnsFalseOrNotFound()
    {
        var owner    = await Data.CreateUserAsync("rm-del-own@e2e.com");
        var intruder = await Data.CreateUserAsync("rm-del-int@e2e.com");
        var roommate = await Data.CreateRoommateAsync(owner.UserId);
        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);

        var response = await new DeleteRoommateEndpoint(client).CallAsync(roommate.RoommateId);

        // Service returns false (not found for this user) → controller returns NotFound
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound,
            HttpStatusCode.Forbidden,
            HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteRoommate_WithoutAuth_Returns401()
    {
        var user     = await Data.CreateUserAsync("rm-del-na@e2e.com");
        var roommate = await Data.CreateRoommateAsync(user.UserId);

        var response = await new DeleteRoommateEndpoint(HttpClient).CallAsync(roommate.RoommateId);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Smoke: new profile fields, filters & list-cache invalidation ─────────

    [Fact]
    public async Task CreateRoommate_WithNewFields_PersistsAndReturnsThem()
    {
        var user   = await Data.CreateUserAsync("rm-newfields@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var create = await new CreateRoommateEndpoint(client).CallAsync(new
        {
            Bio           = "Profil sa novim poljima.",
            Gender        = 2, // Female
            Languages     = "Srpski,Engleski",
            WorkSchedule  = 3, // Night
            MusicFriendly = true,
        });
        create.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("roommateId").GetInt32();

        var get = await new GetRoommateEndpoint(HttpClient).CallAsync(id);
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await get.Content.ReadFromJsonAsync<JsonElement>();

        // Enums are serialized as string names (global JsonStringEnumConverter)
        body.GetProperty("gender").GetString().Should().Be("Female");
        body.GetProperty("languages").GetString().Should().Be("Srpski,Engleski");
        body.GetProperty("workSchedule").GetString().Should().Be("Night");
        body.GetProperty("musicFriendly").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetAllRoommates_FilterByGenderAndWorkSchedule_ReturnsMatchingOnly()
    {
        var userA = await Data.CreateUserAsync("rm-filter-a@e2e.com");
        var userB = await Data.CreateUserAsync("rm-filter-b@e2e.com");
        var clientA = CreateAuthenticatedClient(userA.UserId, userA.UserGuid);
        var clientB = CreateAuthenticatedClient(userB.UserId, userB.UserGuid);

        (await new CreateRoommateEndpoint(clientA).CallAsync(new
        {
            Bio = "Filter test profil A.",
            Profession = "FilterMatch", Gender = 1, WorkSchedule = 2, // Male, Evening
        })).StatusCode.Should().Be(HttpStatusCode.OK);

        (await new CreateRoommateEndpoint(clientB).CallAsync(new
        {
            Bio = "Filter test profil B.",
            Profession = "FilterMiss", Gender = 2, WorkSchedule = 1, // Female, Morning
        })).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await new GetAllRoommatesEndpoint(HttpClient)
            .CallAsync(gender: 1, workSchedule: 2);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body  = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = body.ValueKind == JsonValueKind.Array ? body : body.GetProperty("items");

        items.EnumerateArray().Should().OnlyContain(r =>
            r.GetProperty("gender").GetString() == "Male" &&
            r.GetProperty("workSchedule").GetString() == "Evening");
        items.EnumerateArray().Should().Contain(r =>
            r.GetProperty("profession").GetString() == "FilterMatch");
    }

    [Fact]
    public async Task DeleteRoommate_RemovesProfileFromCachedList()
    {
        var user   = await Data.CreateUserAsync("rm-cacheinv@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        // Create through the API so the list cache is invalidated for the create too
        var create = await new CreateRoommateEndpoint(client).CallAsync(new { Bio = "Cache test profil.", Profession = "CacheTest" });
        create.StatusCode.Should().Be(HttpStatusCode.OK);
        var roommateId = (await create.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("roommateId").GetInt32();

        // Warm the list cache — the profile must be present
        var warm = await new GetAllRoommatesEndpoint(HttpClient).CallAsync();
        warm.StatusCode.Should().Be(HttpStatusCode.OK);
        var warmBody  = await warm.Content.ReadFromJsonAsync<JsonElement>();
        var warmItems = warmBody.ValueKind == JsonValueKind.Array ? warmBody : warmBody.GetProperty("items");
        warmItems.EnumerateArray().Should().Contain(r =>
            r.GetProperty("roommateId").GetInt32() == roommateId);

        (await new DeleteRoommateEndpoint(client).CallAsync(roommateId))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // Deleted profile must disappear immediately (no stale 2–5 min cache)
        var after = await new GetAllRoommatesEndpoint(HttpClient).CallAsync();
        after.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterBody  = await after.Content.ReadFromJsonAsync<JsonElement>();
        var afterItems = afterBody.ValueKind == JsonValueKind.Array ? afterBody : afterBody.GetProperty("items");
        afterItems.EnumerateArray().Should().NotContain(r =>
            r.GetProperty("roommateId").GetInt32() == roommateId);
    }
}
