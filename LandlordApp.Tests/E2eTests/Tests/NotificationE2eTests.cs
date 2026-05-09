using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LandlordApp.Tests.E2eTests.Infrastructure;
using LandlordApp.Tests.E2eTests.Shared;

namespace LandlordApp.Tests.E2eTests.Tests;

/// <summary>
/// E2E tests for the notification CRUD flow.
/// </summary>
[Collection(E2eCollection.Name)]
public class NotificationE2eTests : E2eTestBase
{
    public NotificationE2eTests(E2eFixture fixture) : base(fixture) { }

    // ── GetUserNotifications ──────────────────────────────────────────────────

    [Fact]
    public async Task GetUserNotifications_AsOwner_Returns200WithList()
    {
        var sender    = await Data.CreateUserAsync("notif-sender@e2e.com");
        var recipient = await Data.CreateUserAsync("notif-owner@e2e.com");
        await Data.CreateNotificationAsync(recipient.UserId, sender.UserId, title: "Hello");

        var client   = CreateAuthenticatedClient(recipient.UserId, recipient.UserGuid);
        var response = await new GetUserNotificationsEndpoint(client).CallAsync(recipient.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
        body.EnumerateArray().Should().Contain(n => n.GetProperty("title").GetString() == "Hello");
    }

    [Fact]
    public async Task GetUserNotifications_AsOtherUser_Returns403()
    {
        var sender    = await Data.CreateUserAsync("notif-snd2@e2e.com");
        var recipient = await Data.CreateUserAsync("notif-rcpt2@e2e.com");
        var intruder  = await Data.CreateUserAsync("notif-int2@e2e.com");
        await Data.CreateNotificationAsync(recipient.UserId, sender.UserId);

        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);
        var response = await new GetUserNotificationsEndpoint(client).CallAsync(recipient.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUserNotifications_WithoutAuth_Returns401()
    {
        var recipient = await Data.CreateUserAsync("notif-noauth@e2e.com");

        var response = await new GetUserNotificationsEndpoint(HttpClient).CallAsync(recipient.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserNotifications_ReturnsEmptyList_WhenNoNotifications()
    {
        var user   = await Data.CreateUserAsync("notif-empty@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new GetUserNotificationsEndpoint(client).CallAsync(user.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
        body.GetArrayLength().Should().Be(0);
    }

    // ── MarkAsRead ────────────────────────────────────────────────────────────

    [Fact]
    public async Task MarkAsRead_AsRecipient_Returns200()
    {
        var sender    = await Data.CreateUserAsync("mark-snd@e2e.com");
        var recipient = await Data.CreateUserAsync("mark-rcpt@e2e.com");
        var notif     = await Data.CreateNotificationAsync(recipient.UserId, sender.UserId);

        var client   = CreateAuthenticatedClient(recipient.UserId, recipient.UserGuid);
        var response = await new MarkNotificationAsReadEndpoint(client).CallAsync(notif.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MarkAsRead_AsOtherUser_Returns403()
    {
        var sender    = await Data.CreateUserAsync("mark-snd2@e2e.com");
        var recipient = await Data.CreateUserAsync("mark-rcpt2@e2e.com");
        var intruder  = await Data.CreateUserAsync("mark-int2@e2e.com");
        var notif     = await Data.CreateNotificationAsync(recipient.UserId, sender.UserId);

        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);
        var response = await new MarkNotificationAsReadEndpoint(client).CallAsync(notif.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task MarkAsRead_NonExistentNotification_Returns404()
    {
        var user   = await Data.CreateUserAsync("mark-404@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new MarkNotificationAsReadEndpoint(client).CallAsync(999_999);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MarkAsRead_WithoutAuth_Returns401()
    {
        var sender    = await Data.CreateUserAsync("mark-na-snd@e2e.com");
        var recipient = await Data.CreateUserAsync("mark-na-rcpt@e2e.com");
        var notif     = await Data.CreateNotificationAsync(recipient.UserId, sender.UserId);

        var response = await new MarkNotificationAsReadEndpoint(HttpClient).CallAsync(notif.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── MarkAllAsRead ─────────────────────────────────────────────────────────

    [Fact]
    public async Task MarkAllAsRead_AsAuthenticatedUser_Returns200()
    {
        var sender    = await Data.CreateUserAsync("markall-snd@e2e.com");
        var recipient = await Data.CreateUserAsync("markall-rcpt@e2e.com");
        await Data.CreateNotificationAsync(recipient.UserId, sender.UserId, title: "First");
        await Data.CreateNotificationAsync(recipient.UserId, sender.UserId, title: "Second");

        var client   = CreateAuthenticatedClient(recipient.UserId, recipient.UserGuid);
        var response = await new MarkAllNotificationsAsReadEndpoint(client).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MarkAllAsRead_WithoutAuth_Returns401()
    {
        var response = await new MarkAllNotificationsAsReadEndpoint(HttpClient).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteNotification_AsRecipient_Returns200()
    {
        var sender    = await Data.CreateUserAsync("del-snd@e2e.com");
        var recipient = await Data.CreateUserAsync("del-rcpt@e2e.com");
        var notif     = await Data.CreateNotificationAsync(recipient.UserId, sender.UserId);

        var client   = CreateAuthenticatedClient(recipient.UserId, recipient.UserGuid);
        var response = await new DeleteNotificationEndpoint(client).CallAsync(notif.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteNotification_AsOtherUser_Returns403()
    {
        var sender    = await Data.CreateUserAsync("del-snd2@e2e.com");
        var recipient = await Data.CreateUserAsync("del-rcpt2@e2e.com");
        var intruder  = await Data.CreateUserAsync("del-int2@e2e.com");
        var notif     = await Data.CreateNotificationAsync(recipient.UserId, sender.UserId);

        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);
        var response = await new DeleteNotificationEndpoint(client).CallAsync(notif.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteNotification_NonExistent_Returns404()
    {
        var user   = await Data.CreateUserAsync("del-404@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new DeleteNotificationEndpoint(client).CallAsync(999_999);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteNotification_WithoutAuth_Returns401()
    {
        var sender    = await Data.CreateUserAsync("del-na-snd@e2e.com");
        var recipient = await Data.CreateUserAsync("del-na-rcpt@e2e.com");
        var notif     = await Data.CreateNotificationAsync(recipient.UserId, sender.UserId);

        var response = await new DeleteNotificationEndpoint(HttpClient).CallAsync(notif.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
