using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LandlordApp.Tests.E2eTests.Infrastructure;

namespace LandlordApp.Tests.E2eTests.Tests;

/// <summary>
/// Two people agreeing to be roommates: proposing, answering, becoming "taken" in the roommate
/// search, and the shortlist of apartments they share.
/// </summary>
[Collection(E2eCollection.Name)]
public class RoommatePairingE2eTests : E2eTestBase
{
    private const string Base = "/api/v1/roommate-pairing";

    public RoommatePairingE2eTests(E2eFixture fixture) : base(fixture) { }

    private static async Task<JsonElement> Json(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    private static async Task<string> CodeOf(HttpResponseMessage response) =>
        (await Json(response)).GetProperty("code").GetString()!;

    private async Task<(HttpClient client, int userId)> NewUser(string email)
    {
        var user = await Data.CreateUserAsync(email);
        return (CreateAuthenticatedClient(user.UserId, user.UserGuid), user.UserId);
    }

    private static async Task<int> Propose(HttpClient from, int toUserId)
    {
        var response = await from.PostAsJsonAsync($"{Base}/propose", new { PartnerUserId = toUserId });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await Json(response)).GetProperty("pairingId").GetInt32();
    }

    private async Task Pair(HttpClient a, HttpClient b, int bUserId)
    {
        var pairingId = await Propose(a, bUserId);
        (await b.PostAsync($"{Base}/{pairingId}/accept", null)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<IReadOnlyList<int>> ListedUserIds(HttpClient client, string location, bool includeTaken = false)
    {
        var url = $"/api/v1/roommates/get-all-roommates?page=1&pageSize=50&location={Uri.EscapeDataString(location)}";
        if (includeTaken) url += "&includeTaken=true";
        var body = await Json(await client.GetAsync(url));
        return body.GetProperty("items").EnumerateArray().Select(r => r.GetProperty("userId").GetInt32()).ToList();
    }

    // ── Proposing and answering ───────────────────────────────────────────────

    [Fact]
    public async Task Propose_WithoutAuth_Returns401()
    {
        var response = await HttpClient.PostAsJsonAsync($"{Base}/propose", new { PartnerUserId = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Propose_ShowsUpAsOutgoingForMe_AndIncomingForThem()
    {
        var (ana, _) = await NewUser("pair-a1@e2e.com");
        var (marko, markoId) = await NewUser("pair-b1@e2e.com");

        await Propose(ana, markoId);

        var mine = await Json(await ana.GetAsync($"{Base}/my"));
        mine.GetProperty("outgoing").GetArrayLength().Should().Be(1);
        mine.GetProperty("incoming").GetArrayLength().Should().Be(0);
        mine.GetProperty("active").ValueKind.Should().Be(JsonValueKind.Null);

        var theirs = await Json(await marko.GetAsync($"{Base}/my"));
        theirs.GetProperty("incoming").GetArrayLength().Should().Be(1);
        theirs.GetProperty("incoming")[0].GetProperty("isIncoming").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Propose_ToYourself_Returns422WithCode()
    {
        var (ana, anaId) = await NewUser("pair-self@e2e.com");

        var response = await ana.PostAsJsonAsync($"{Base}/propose", new { PartnerUserId = anaId });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await CodeOf(response)).Should().Be("PAIRING_WITH_SELF");
    }

    [Fact]
    public async Task Accept_MakesBothActive_AndOnlyTheOneAskedMayAccept()
    {
        var (ana, anaId) = await NewUser("pair-a2@e2e.com");
        var (marko, markoId) = await NewUser("pair-b2@e2e.com");
        var pairingId = await Propose(ana, markoId);

        (await ana.PostAsync($"{Base}/{pairingId}/accept", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await marko.PostAsync($"{Base}/{pairingId}/accept", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var anasView = await Json(await ana.GetAsync($"{Base}/my"));
        anasView.GetProperty("active").GetProperty("partnerUserId").GetInt32().Should().Be(markoId);
        var markosView = await Json(await marko.GetAsync($"{Base}/my"));
        markosView.GetProperty("active").GetProperty("partnerUserId").GetInt32().Should().Be(anaId);
    }

    [Fact]
    public async Task Accept_TellsTheProposerWithABellNotification()
    {
        var (ana, anaId) = await NewUser("pair-a3@e2e.com");
        var (marko, markoId) = await NewUser("pair-b3@e2e.com");
        await Pair(ana, marko, markoId);

        var notifications = await Json(await ana.GetAsync($"/api/v1/notification/get-user-notifications?id={anaId}"));

        notifications.EnumerateArray()
            .Select(n => n.GetProperty("title").GetString())
            .Should().Contain(title => title!.Contains("Cimerstvo potvrđeno"));
    }

    [Fact]
    public async Task Propose_StoresATranslatableNotification_TheRecipientFindsLaterAndCanMarkRead()
    {
        var (ana, _) = await NewUser("pair-a12@e2e.com");
        var (marko, markoId) = await NewUser("pair-b12@e2e.com");
        await Propose(ana, markoId);

        // Marko was away: the notification is waiting for him, with its key and values, not just text.
        var waiting = await Json(await marko.GetAsync($"/api/v1/notification/get-user-notifications?id={markoId}"));
        var item = waiting.EnumerateArray().Single(n => n.GetProperty("key").GetString() == "pairing_proposed");
        item.GetProperty("params").GetProperty("name").GetString().Should().Be("Test User");
        item.GetProperty("actionTarget").GetString().Should().Be("/my-roommate");

        var id = item.GetProperty("id").GetInt32();
        (await marko.PostAsync($"/api/v1/notification/mark-as-read?notificationId={id}", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var after = await Json(await marko.GetAsync($"/api/v1/notification/get-user-notifications?id={markoId}"));
        after.EnumerateArray().Should().NotContain(n => n.GetProperty("id").GetInt32() == id);
    }

    [Fact]
    public async Task Propose_ToSomeoneAlreadyTaken_Returns422WithCode()
    {
        var (marko, markoId) = await NewUser("pair-b4@e2e.com");
        var (petar, _) = await NewUser("pair-e4@e2e.com");
        var (jovana, _) = await NewUser("pair-c4@e2e.com");
        await Pair(petar, marko, markoId);

        var response = await jovana.PostAsJsonAsync($"{Base}/propose", new { PartnerUserId = markoId });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await CodeOf(response)).Should().Be("ROOMMATE_ALREADY_TAKEN");
    }

    [Fact]
    public async Task Propose_WhenIAmTaken_Returns422WithCode()
    {
        var (ana, _) = await NewUser("pair-a5@e2e.com");
        var (marko, markoId) = await NewUser("pair-b5@e2e.com");
        var (_, jovanaId) = await NewUser("pair-c5@e2e.com");
        await Pair(ana, marko, markoId);

        var response = await ana.PostAsJsonAsync($"{Base}/propose", new { PartnerUserId = jovanaId });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await CodeOf(response)).Should().Be("YOU_ALREADY_HAVE_ROOMMATE");
    }

    [Fact]
    public async Task Decline_And_Cancel_CloseTheProposal()
    {
        var (ana, _) = await NewUser("pair-a6@e2e.com");
        var (marko, markoId) = await NewUser("pair-b6@e2e.com");
        var (jovana, jovanaId) = await NewUser("pair-c6@e2e.com");

        var declined = await Propose(ana, markoId);
        (await marko.PostAsync($"{Base}/{declined}/decline", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var withdrawn = await Propose(ana, jovanaId);
        (await ana.PostAsync($"{Base}/{withdrawn}/cancel", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await Json(await ana.GetAsync($"{Base}/my"))).GetProperty("outgoing").GetArrayLength().Should().Be(0);
        (await Json(await jovana.GetAsync($"{Base}/my"))).GetProperty("incoming").GetArrayLength().Should().Be(0);
        // answering twice is refused
        var again = await marko.PostAsync($"{Base}/{declined}/decline", null);
        again.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await CodeOf(again)).Should().Be("PAIRING_NOT_PENDING");
    }

    // ── Taken in the roommate search ─────────────────────────────────────────

    [Fact]
    public async Task AcceptedPairing_RemovesBothFromRoommateSearch_AndEndingPutsThemBack()
    {
        const string location = "Parovac";
        var (ana, anaId) = await NewUser("pair-a7@e2e.com");
        var (marko, markoId) = await NewUser("pair-b7@e2e.com");
        var (jovana, jovanaId) = await NewUser("pair-c7@e2e.com");
        foreach (var id in new[] { anaId, markoId, jovanaId })
            await Data.CreateRoommateAsync(id, location: location);

        (await ListedUserIds(jovana, location)).Should().BeEquivalentTo(new[] { anaId, markoId, jovanaId });

        var pairingId = await Propose(ana, markoId);
        await marko.PostAsync($"{Base}/{pairingId}/accept", null);

        (await ListedUserIds(jovana, location)).Should().BeEquivalentTo(new[] { jovanaId });

        // still reachable when asked for explicitly, and marked
        var withTaken = await Json(await jovana.GetAsync(
            $"/api/v1/roommates/get-all-roommates?page=1&pageSize=50&includeTaken=true&location={location}"));
        var anaEntry = withTaken.GetProperty("items").EnumerateArray().Single(r => r.GetProperty("userId").GetInt32() == anaId);
        anaEntry.GetProperty("isTaken").GetBoolean().Should().BeTrue();

        // and the profile itself says so
        var profile = await Json(await HttpClient.GetAsync($"/api/v1/roommates/get-roommate-by-user-id?userId={anaId}"));
        profile.GetProperty("isTaken").GetBoolean().Should().BeTrue();

        (await ana.PostAsync($"{Base}/{pairingId}/end", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await ListedUserIds(jovana, location)).Should().BeEquivalentTo(new[] { anaId, markoId, jovanaId });
    }

    // ── Shared apartment list ────────────────────────────────────────────────

    [Fact]
    public async Task ShareApartment_WithoutARoommate_Returns422WithCode()
    {
        var (ana, _) = await NewUser("pair-a8@e2e.com");
        var landlord = await Data.CreateUserAsync("pair-l8@e2e.com", role: "Landlord");
        var apartment = await Data.CreateApartmentAsync(landlord.UserId);

        var response = await ana.PostAsJsonAsync($"{Base}/shared-apartments", new { ApartmentId = apartment.ApartmentId });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await CodeOf(response)).Should().Be("PAIRING_REQUIRED");
    }

    [Fact]
    public async Task ShareApartment_IsVisibleToBothRoommates_AndEitherMayRemoveIt()
    {
        var (ana, anaId) = await NewUser("pair-a9@e2e.com");
        var (marko, markoId) = await NewUser("pair-b9@e2e.com");
        var landlord = await Data.CreateUserAsync("pair-l9@e2e.com", role: "Landlord");
        var apartment = await Data.CreateApartmentAsync(landlord.UserId, title: "Stan za dvoje");
        await Pair(ana, marko, markoId);

        var shared = await ana.PostAsJsonAsync($"{Base}/shared-apartments",
            new { ApartmentId = apartment.ApartmentId, Note = "Blizu posla" });
        shared.StatusCode.Should().Be(HttpStatusCode.OK);

        var markosList = await Json(await marko.GetAsync($"{Base}/shared-apartments"));
        markosList.GetArrayLength().Should().Be(1);
        markosList[0].GetProperty("title").GetString().Should().Be("Stan za dvoje");
        markosList[0].GetProperty("note").GetString().Should().Be("Blizu posla");
        markosList[0].GetProperty("addedByUserId").GetInt32().Should().Be(anaId);
        markosList[0].GetProperty("isAvailable").GetBoolean().Should().BeTrue();

        // sharing again changes nothing
        (await marko.PostAsJsonAsync($"{Base}/shared-apartments", new { ApartmentId = apartment.ApartmentId }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await Json(await ana.GetAsync($"{Base}/shared-apartments"))).GetArrayLength().Should().Be(1);

        (await marko.DeleteAsync($"{Base}/shared-apartments/{apartment.ApartmentId}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ana.DeleteAsync($"{Base}/shared-apartments/{apartment.ApartmentId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Json(await ana.GetAsync($"{Base}/shared-apartments"))).GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task ShareApartment_ThatDoesNotExist_Returns404()
    {
        var (ana, _) = await NewUser("pair-a10@e2e.com");
        var (marko, markoId) = await NewUser("pair-b10@e2e.com");
        await Pair(ana, marko, markoId);

        var response = await ana.PostAsJsonAsync($"{Base}/shared-apartments", new { ApartmentId = 987654 });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SharedList_IsEmptyForSomeoneWithoutARoommate()
    {
        var (ana, _) = await NewUser("pair-a11@e2e.com");

        var list = await Json(await ana.GetAsync($"{Base}/shared-apartments"));

        list.GetArrayLength().Should().Be(0);
    }
}
