using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LandlordApp.Tests.IntegrationTests.Infrastructure;

namespace LandlordApp.Tests.IntegrationTests.Chat;

/// <summary>
/// Verifies that ChatHistoryConsent is enforced at the HTTP layer.
/// These tests directly confirm the fix implemented in MessageService.Conversations.cs.
/// </summary>
public class ChatHistoryConsentTests : IntegrationTestBase, IClassFixture<LanderWebApplicationFactory>
{
    public ChatHistoryConsentTests(LanderWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetConversation_BothUsersConsentTrue_ReturnsMessages()
    {
        var userA = await SeedUserAsync("consent-a1@test.com", chatHistoryConsent: true);
        var userB = await SeedUserAsync("consent-b1@test.com", chatHistoryConsent: true);
        await SeedMessageAsync(userA.UserId, userB.UserId, "Hello B!");
        await SeedMessageAsync(userB.UserId, userA.UserId, "Hello A!");

        var client = CreateAuthenticatedClient(userA.UserId, userA.UserGuid);
        var response = await client.GetAsync(
            $"/api/v1/messages/conversation?userId1={userA.UserId}&userId2={userB.UserId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().Should().Be(2);
        body.GetProperty("messages").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task GetConversation_ReceiverHasConsentFalse_ReturnsEmptyMessages()
    {
        var userA = await SeedUserAsync("consent-a2@test.com", chatHistoryConsent: true);
        var userB = await SeedUserAsync("consent-b2@test.com", chatHistoryConsent: false);
        await SeedMessageAsync(userA.UserId, userB.UserId, "This should be hidden");

        var client = CreateAuthenticatedClient(userA.UserId, userA.UserGuid);
        var response = await client.GetAsync(
            $"/api/v1/messages/conversation?userId1={userA.UserId}&userId2={userB.UserId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().Should().Be(0);
        body.GetProperty("messages").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetConversation_SenderHasConsentFalse_ReturnsEmptyMessages()
    {
        var userA = await SeedUserAsync("consent-a3@test.com", chatHistoryConsent: false);
        var userB = await SeedUserAsync("consent-b3@test.com", chatHistoryConsent: true);
        await SeedMessageAsync(userA.UserId, userB.UserId, "Also hidden");

        // userB queries the conversation — but userA has consent=false
        var client = CreateAuthenticatedClient(userB.UserId, userB.UserGuid);
        var response = await client.GetAsync(
            $"/api/v1/messages/conversation?userId1={userA.UserId}&userId2={userB.UserId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCount").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task GetConversation_ConsentChangedAfterMessagesExist_HidesRetroactively()
    {
        // Start with both consenting
        var userA = await SeedUserAsync("consent-a4@test.com", chatHistoryConsent: true);
        var userB = await SeedUserAsync("consent-b4@test.com", chatHistoryConsent: true);
        await SeedMessageAsync(userA.UserId, userB.UserId, "Message before revoke");

        var client = CreateAuthenticatedClient(userA.UserId, userA.UserGuid);

        // Confirm messages visible
        var beforeResp = await client.GetAsync(
            $"/api/v1/messages/conversation?userId1={userA.UserId}&userId2={userB.UserId}");
        var before = await beforeResp.Content.ReadFromJsonAsync<JsonElement>();
        before.GetProperty("totalCount").GetInt32().Should().Be(1);

        // UserB revokes consent
        await SetChatHistoryConsentAsync(userB.UserId, false);

        // Same query must now return empty
        var afterResp = await client.GetAsync(
            $"/api/v1/messages/conversation?userId1={userA.UserId}&userId2={userB.UserId}");
        var after = await afterResp.Content.ReadFromJsonAsync<JsonElement>();
        after.GetProperty("totalCount").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task GetUserConversations_OtherUserConsentFalse_HidesLastMessage()
    {
        var userA = await SeedUserAsync("conv-list-a@test.com", chatHistoryConsent: true);
        var userB = await SeedUserAsync("conv-list-b@test.com", chatHistoryConsent: false);
        await SeedMessageAsync(userA.UserId, userB.UserId, "Hidden last message");

        var client = CreateAuthenticatedClient(userA.UserId, userA.UserGuid);
        var response = await client.GetAsync($"/api/v1/messages/user/{userA.UserId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var conversations = await response.Content.ReadFromJsonAsync<JsonElement>();
        var firstConv = conversations.EnumerateArray().FirstOrDefault();
        // lastMessage must be null when the other user has consent=false
        firstConv.TryGetProperty("lastMessage", out var lastMessage).Should().BeTrue();
        lastMessage.ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetConversation_ThirdPartyRequest_Returns403()
    {
        var userA      = await SeedUserAsync("third-a@test.com");
        var userB      = await SeedUserAsync("third-b@test.com");
        var intruder   = await SeedUserAsync("third-c@test.com");

        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);
        var response = await client.GetAsync(
            $"/api/v1/messages/conversation?userId1={userA.UserId}&userId2={userB.UserId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
