using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LandlordApp.Tests.E2eTests.Infrastructure;

namespace LandlordApp.Tests.E2eTests.Tests;

/// <summary>
/// The conversation of two roommates with the landlord they both talk to: opening it carries the
/// earlier conversation over, everybody sees the same messages, and it counts in the inbox badge.
/// </summary>
[Collection(E2eCollection.Name)]
public class ChatGroupE2eTests : E2eTestBase
{
    private const string Groups = "/api/v1/chat-groups";
    private const string Pairing = "/api/v1/roommate-pairing";

    public ChatGroupE2eTests(E2eFixture fixture) : base(fixture) { }

    private static async Task<JsonElement> Json(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    private sealed record Person(HttpClient Client, int UserId);

    private async Task<Person> NewPerson(string email, string role = "Tenant")
    {
        var user = await Data.CreateUserAsync(email, role: role);
        return new Person(CreateAuthenticatedClient(user.UserId, user.UserGuid, role), user.UserId);
    }

    /// <summary>Two roommates and a landlord; the first roommate has already written to the landlord.</summary>
    private async Task<(Person ana, Person marko, Person landlord)> Scenario(string tag)
    {
        var ana = await NewPerson($"grp-a-{tag}@e2e.com");
        var marko = await NewPerson($"grp-b-{tag}@e2e.com");
        var landlord = await NewPerson($"grp-l-{tag}@e2e.com", "Landlord");

        var proposal = await Json(await ana.Client.PostAsJsonAsync($"{Pairing}/propose", new { PartnerUserId = marko.UserId }));
        (await marko.Client.PostAsync($"{Pairing}/{proposal.GetProperty("pairingId").GetInt32()}/accept", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        await Data.CreateMessageAsync(ana.UserId, landlord.UserId, "Zanima me stan", isRead: true);
        await Data.CreateMessageAsync(landlord.UserId, ana.UserId, "Slobodan je od prvog", isRead: true);
        return (ana, marko, landlord);
    }

    private static async Task<int> Open(Person who, int otherUserId)
    {
        var response = await who.Client.PostAsJsonAsync($"{Groups}/open", new { OtherUserId = otherUserId });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await Json(response)).GetProperty("groupId").GetInt32();
    }

    private static async Task<List<string>> TextsSeenBy(Person who, int groupId)
    {
        var page = await Json(await who.Client.GetAsync($"{Groups}/{groupId}/messages"));
        return page.GetProperty("messages").EnumerateArray().Select(m => m.GetProperty("messageText").GetString()!).ToList();
    }

    [Fact]
    public async Task Groups_WithoutAuth_Returns401()
    {
        (await HttpClient.GetAsync(Groups)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Open_WithoutARoommate_Returns422WithCode()
    {
        var ana = await NewPerson("grp-a-solo@e2e.com");
        var landlord = await NewPerson("grp-l-solo@e2e.com", "Landlord");
        await Data.CreateMessageAsync(ana.UserId, landlord.UserId, "Zdravo");

        var response = await ana.Client.PostAsJsonAsync($"{Groups}/open", new { OtherUserId = landlord.UserId });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await Json(response)).GetProperty("code").GetString().Should().Be("PAIRING_REQUIRED");
    }

    [Fact]
    public async Task Open_WithSomeoneNobodyHasTalkedTo_Returns422WithCode()
    {
        var (ana, _, _) = await Scenario("nohist");
        var stranger = await NewPerson("grp-s-nohist@e2e.com");

        var response = await ana.Client.PostAsJsonAsync($"{Groups}/open", new { OtherUserId = stranger.UserId });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await Json(response)).GetProperty("code").GetString().Should().Be("GROUP_CHAT_NOT_ALLOWED");
    }

    [Fact]
    public async Task Open_WithTheLandlordOfASharedApartment_NeedsNoEarlierConversation()
    {
        var ana = await NewPerson("grp-a-shared@e2e.com");
        var marko = await NewPerson("grp-b-shared@e2e.com");
        var landlord = await NewPerson("grp-l-shared@e2e.com", "Landlord");
        var proposal = await Json(await ana.Client.PostAsJsonAsync($"{Pairing}/propose", new { PartnerUserId = marko.UserId }));
        await marko.Client.PostAsync($"{Pairing}/{proposal.GetProperty("pairingId").GetInt32()}/accept", null);
        var apartment = await Data.CreateApartmentAsync(landlord.UserId, title: "Stan sa balkonom");

        // nobody has written to the landlord yet, and the apartment is not on the list yet
        var before = await ana.Client.PostAsJsonAsync($"{Groups}/open", new { OtherUserId = landlord.UserId });
        before.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await Json(before)).GetProperty("code").GetString().Should().Be("GROUP_CHAT_NOT_ALLOWED");

        await ana.Client.PostAsJsonAsync($"{Pairing}/shared-apartments", new { ApartmentId = apartment.ApartmentId });

        // the shared list tells who the landlord is...
        var list = await Json(await marko.Client.GetAsync($"{Pairing}/shared-apartments"));
        list[0].GetProperty("landlordUserId").GetInt32().Should().Be(landlord.UserId);
        list[0].GetProperty("landlordName").GetString().Should().Be("Test User");

        // ...and that is enough to open the shared conversation with them
        var groupId = await Open(marko, landlord.UserId);
        (await TextsSeenBy(landlord, groupId)).Should().BeEmpty();
        (await landlord.Client.PostAsJsonAsync($"{Groups}/{groupId}/messages", new { MessageText = "Stan je slobodan" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await TextsSeenBy(ana, groupId)).Should().Equal("Stan je slobodan");
    }

    [Fact]
    public async Task Open_ReturnsTheGroupOfThree_AndTheRoommateReadsWhatWasSaidBeforeHeJoined()
    {
        var (ana, marko, landlord) = await Scenario("open");

        var response = await ana.Client.PostAsJsonAsync($"{Groups}/open", new { OtherUserId = landlord.UserId });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var group = await Json(response);
        group.GetProperty("members").GetArrayLength().Should().Be(3);
        var groupId = group.GetProperty("groupId").GetInt32();

        (await TextsSeenBy(marko, groupId)).Should().Equal("Zanima me stan", "Slobodan je od prvog");
        (await TextsSeenBy(landlord, groupId)).Should().Equal("Zanima me stan", "Slobodan je od prvog");
    }

    [Fact]
    public async Task Open_Twice_GivesTheSameGroup_EvenFromTheOtherRoommate()
    {
        var (ana, marko, landlord) = await Scenario("twice");

        var first = await Open(ana, landlord.UserId);
        var second = await Open(marko, landlord.UserId);

        second.Should().Be(first);
        var mine = await Json(await ana.Client.GetAsync(Groups));
        mine.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task Everybody_SeesWhatAnyMemberWrites_AndItCountsInTheInboxBadge()
    {
        var (ana, marko, landlord) = await Scenario("send");
        var groupId = await Open(ana, landlord.UserId);

        var sent = await marko.Client.PostAsJsonAsync($"{Groups}/{groupId}/messages", new { MessageText = "Može u subotu?" });
        sent.StatusCode.Should().Be(HttpStatusCode.OK);

        (await TextsSeenBy(landlord, groupId)).Should().EndWith("Može u subotu?");
        (await TextsSeenBy(ana, groupId)).Should().EndWith("Može u subotu?");

        // the landlord has one unread message: it shows in the same badge as one-to-one messages
        var unread = await landlord.Client.GetAsync($"/api/v1/messages/unread-count/{landlord.UserId}");
        (await unread.Content.ReadFromJsonAsync<int>()).Should().Be(1);
        var mine = await Json(await landlord.Client.GetAsync(Groups));
        mine[0].GetProperty("unreadCount").GetInt32().Should().Be(1);
        mine[0].GetProperty("lastMessage").GetProperty("messageText").GetString().Should().Be("Može u subotu?");

        (await landlord.Client.PostAsync($"{Groups}/{groupId}/read", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var after = await landlord.Client.GetAsync($"/api/v1/messages/unread-count/{landlord.UserId}");
        (await after.Content.ReadFromJsonAsync<int>()).Should().Be(0);
    }

    [Fact]
    public async Task ANonMember_CannotReadOrWrite()
    {
        var (ana, _, landlord) = await Scenario("outsider");
        var groupId = await Open(ana, landlord.UserId);
        var stranger = await NewPerson("grp-s-outsider@e2e.com");

        (await stranger.Client.GetAsync($"{Groups}/{groupId}/messages")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await stranger.Client.PostAsJsonAsync($"{Groups}/{groupId}/messages", new { MessageText = "Upadam" }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await stranger.Client.PostAsync($"{Groups}/{groupId}/leave", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AnEmptyMessage_IsRejectedWithItsCode()
    {
        var (ana, _, landlord) = await Scenario("empty");
        var groupId = await Open(ana, landlord.UserId);

        var response = await ana.Client.PostAsJsonAsync($"{Groups}/{groupId}/messages", new { MessageText = "   " });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("MESSAGE_EMPTY");
    }

    [Fact]
    public async Task Leaving_RemovesThePersonFromTheirList_AndOthersKeepTheGroup()
    {
        var (ana, marko, landlord) = await Scenario("leave");
        var groupId = await Open(ana, landlord.UserId);

        (await marko.Client.PostAsync($"{Groups}/{groupId}/leave", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await Json(await marko.Client.GetAsync(Groups))).GetArrayLength().Should().Be(0);
        (await marko.Client.GetAsync($"{Groups}/{groupId}/messages")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await TextsSeenBy(ana, groupId)).Should().NotBeEmpty();
    }
}
