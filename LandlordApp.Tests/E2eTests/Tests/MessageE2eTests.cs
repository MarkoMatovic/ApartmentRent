using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LandlordApp.Tests.E2eTests.Infrastructure;
using LandlordApp.Tests.E2eTests.Shared;

namespace LandlordApp.Tests.E2eTests.Tests;

/// <summary>
/// E2E tests for the messaging flow.
/// </summary>
[Collection(E2eCollection.Name)]
public class MessageE2eTests : E2eTestBase
{
    public MessageE2eTests(E2eFixture fixture) : base(fixture) { }

    // ── SendMessage ───────────────────────────────────────────────────────────

    [Fact]
    public async Task SendMessage_AsAuthenticatedUser_Returns200WithMessage()
    {
        var sender   = await Data.CreateUserAsync("msg-send-a@e2e.com");
        var receiver = await Data.CreateUserAsync("msg-send-b@e2e.com");
        var client   = CreateAuthenticatedClient(sender.UserId, sender.UserGuid);

        var response = await new SendMessageEndpoint(client).CallAsync(receiver.UserId, "Zdravo!");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("messageText").GetString().Should().Be("Zdravo!");
    }

    [Fact]
    public async Task SendMessage_WithoutAuth_Returns401()
    {
        var receiver = await Data.CreateUserAsync("msg-send-na@e2e.com");

        var response = await new SendMessageEndpoint(HttpClient).CallAsync(receiver.UserId, "Zdravo!");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GetConversation ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetConversation_AsSenderOrReceiver_Returns200WithMessages()
    {
        var userA = await Data.CreateUserAsync("msg-conv-a@e2e.com");
        var userB = await Data.CreateUserAsync("msg-conv-b@e2e.com");
        await Data.CreateMessageAsync(userA.UserId, userB.UserId, "Poruka 1");
        await Data.CreateMessageAsync(userB.UserId, userA.UserId, "Poruka 2");

        var client   = CreateAuthenticatedClient(userA.UserId, userA.UserGuid);
        var response = await new GetConversationEndpoint(client).CallAsync(userA.UserId, userB.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Response might be paged { messages: [...] } or direct array
        var messages = body.ValueKind == JsonValueKind.Array
            ? body
            : body.GetProperty("messages");

        messages.GetArrayLength().Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetConversation_AsThirdParty_Returns403()
    {
        var userA     = await Data.CreateUserAsync("msg-conv-a2@e2e.com");
        var userB     = await Data.CreateUserAsync("msg-conv-b2@e2e.com");
        var intruder  = await Data.CreateUserAsync("msg-conv-int@e2e.com");
        await Data.CreateMessageAsync(userA.UserId, userB.UserId, "Tajna poruka");

        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);
        var response = await new GetConversationEndpoint(client).CallAsync(userA.UserId, userB.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetConversation_WithoutAuth_Returns401()
    {
        var userA = await Data.CreateUserAsync("msg-conv-na-a@e2e.com");
        var userB = await Data.CreateUserAsync("msg-conv-na-b@e2e.com");

        var response = await new GetConversationEndpoint(HttpClient).CallAsync(userA.UserId, userB.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GetUserConversations ──────────────────────────────────────────────────

    [Fact]
    public async Task GetUserConversations_AsOwner_Returns200()
    {
        var user  = await Data.CreateUserAsync("msg-uc-a@e2e.com");
        var other = await Data.CreateUserAsync("msg-uc-b@e2e.com");
        await Data.CreateMessageAsync(user.UserId, other.UserId, "Zdravo");

        var client   = CreateAuthenticatedClient(user.UserId, user.UserGuid);
        var response = await new GetUserConversationsEndpoint(client).CallAsync(user.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUserConversations_AsOtherUser_Returns403()
    {
        var owner    = await Data.CreateUserAsync("msg-uc-own@e2e.com");
        var intruder = await Data.CreateUserAsync("msg-uc-int@e2e.com");

        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);
        var response = await new GetUserConversationsEndpoint(client).CallAsync(owner.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUserConversations_WithoutAuth_Returns401()
    {
        var user = await Data.CreateUserAsync("msg-uc-na@e2e.com");

        var response = await new GetUserConversationsEndpoint(HttpClient).CallAsync(user.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GetUnreadCount ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUnreadCount_AsOwner_Returns200WithCount()
    {
        var sender   = await Data.CreateUserAsync("msg-unread-snd@e2e.com");
        var receiver = await Data.CreateUserAsync("msg-unread-rcv@e2e.com");
        await Data.CreateMessageAsync(sender.UserId, receiver.UserId, "Nepročitano", isRead: false);

        var client   = CreateAuthenticatedClient(receiver.UserId, receiver.UserGuid);
        var response = await new GetUnreadCountEndpoint(client).CallAsync(receiver.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var count = await response.Content.ReadFromJsonAsync<int>();
        count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetUnreadCount_AsOtherUser_Returns403()
    {
        var owner    = await Data.CreateUserAsync("msg-unread-own@e2e.com");
        var intruder = await Data.CreateUserAsync("msg-unread-int@e2e.com");

        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);
        var response = await new GetUnreadCountEndpoint(client).CallAsync(owner.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── MarkAsRead ────────────────────────────────────────────────────────────

    [Fact]
    public async Task MarkMessageAsRead_AsRecipient_Returns200()
    {
        var sender   = await Data.CreateUserAsync("msg-read-snd@e2e.com");
        var receiver = await Data.CreateUserAsync("msg-read-rcv@e2e.com");
        var msg      = await Data.CreateMessageAsync(sender.UserId, receiver.UserId, "Pročitaj me");

        var client   = CreateAuthenticatedClient(receiver.UserId, receiver.UserGuid);
        var response = await new MarkMessageAsReadEndpoint(client).CallAsync(msg.MessageId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MarkMessageAsRead_AsNonRecipient_Returns403()
    {
        var sender   = await Data.CreateUserAsync("msg-read-snd2@e2e.com");
        var receiver = await Data.CreateUserAsync("msg-read-rcv2@e2e.com");
        var intruder = await Data.CreateUserAsync("msg-read-int@e2e.com");
        var msg      = await Data.CreateMessageAsync(sender.UserId, receiver.UserId, "Tajna");

        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);
        var response = await new MarkMessageAsReadEndpoint(client).CallAsync(msg.MessageId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task MarkMessageAsRead_WithoutAuth_Returns401()
    {
        var sender   = await Data.CreateUserAsync("msg-read-na-s@e2e.com");
        var receiver = await Data.CreateUserAsync("msg-read-na-r@e2e.com");
        var msg      = await Data.CreateMessageAsync(sender.UserId, receiver.UserId);

        var response = await new MarkMessageAsReadEndpoint(HttpClient).CallAsync(msg.MessageId);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Archive ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task ArchiveConversation_AsOwner_Returns200()
    {
        var user  = await Data.CreateUserAsync("msg-arch-a@e2e.com");
        var other = await Data.CreateUserAsync("msg-arch-b@e2e.com");

        var client   = CreateAuthenticatedClient(user.UserId, user.UserGuid);
        var response = await new ArchiveConversationEndpoint(client).CallAsync(user.UserId, other.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ArchiveConversation_AsOtherUser_Returns403()
    {
        var user     = await Data.CreateUserAsync("msg-arch-own@e2e.com");
        var other    = await Data.CreateUserAsync("msg-arch-oth@e2e.com");
        var intruder = await Data.CreateUserAsync("msg-arch-int@e2e.com");

        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);
        var response = await new ArchiveConversationEndpoint(client).CallAsync(user.UserId, other.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Block ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task BlockUser_AsOwner_Returns200()
    {
        var user  = await Data.CreateUserAsync("msg-blk-a@e2e.com");
        var other = await Data.CreateUserAsync("msg-blk-b@e2e.com");

        var client   = CreateAuthenticatedClient(user.UserId, user.UserGuid);
        var response = await new BlockUserEndpoint(client).CallAsync(user.UserId, other.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task BlockUser_AsOtherUser_Returns403()
    {
        var user     = await Data.CreateUserAsync("msg-blk-own@e2e.com");
        var other    = await Data.CreateUserAsync("msg-blk-oth@e2e.com");
        var intruder = await Data.CreateUserAsync("msg-blk-int@e2e.com");

        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);
        var response = await new BlockUserEndpoint(client).CallAsync(user.UserId, other.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── DeleteConversation ────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteConversation_AsAuthenticated_Returns200()
    {
        var user  = await Data.CreateUserAsync("msg-del-a@e2e.com");
        var other = await Data.CreateUserAsync("msg-del-b@e2e.com");
        await Data.CreateMessageAsync(user.UserId, other.UserId, "Poruka za brisanje");

        var client   = CreateAuthenticatedClient(user.UserId, user.UserGuid);
        var response = await new DeleteConversationEndpoint(client).CallAsync(other.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteConversation_WithoutAuth_Returns401()
    {
        var other = await Data.CreateUserAsync("msg-del-na@e2e.com");

        var response = await new DeleteConversationEndpoint(HttpClient).CallAsync(other.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
