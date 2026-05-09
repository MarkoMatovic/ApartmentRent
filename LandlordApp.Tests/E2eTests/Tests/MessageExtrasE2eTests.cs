using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LandlordApp.Tests.E2eTests.Infrastructure;
using LandlordApp.Tests.E2eTests.Shared;

namespace LandlordApp.Tests.E2eTests.Tests;

/// <summary>
/// E2E tests for messaging endpoints not covered in MessageE2eTests:
///   – UnarchiveConversation
///   – MuteConversation / UnmuteConversation
///   – UnblockUser
///   – SearchMessages
///   – ReportAbuse
/// </summary>
[Collection(E2eCollection.Name)]
public class MessageExtrasE2eTests : E2eTestBase
{
    public MessageExtrasE2eTests(E2eFixture fixture) : base(fixture) { }

    // ── UnarchiveConversation ─────────────────────────────────────────────────

    [Fact]
    public async Task UnarchiveConversation_AsOwner_Returns200()
    {
        var u1     = await Data.CreateUserAsync("msg-unarc-u1@e2e.com");
        var u2     = await Data.CreateUserAsync("msg-unarc-u2@e2e.com");
        var client = CreateAuthenticatedClient(u1.UserId, u1.UserGuid);

        // Archive first
        await new ArchiveConversationEndpoint(client).CallAsync(u1.UserId, u2.UserId);

        var response = await new UnarchiveConversationEndpoint(client).CallAsync(u1.UserId, u2.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UnarchiveConversation_AsOtherUser_Returns403()
    {
        var u1       = await Data.CreateUserAsync("msg-unarc-own@e2e.com");
        var u2       = await Data.CreateUserAsync("msg-unarc-other@e2e.com");
        var intruder = await Data.CreateUserAsync("msg-unarc-int@e2e.com");
        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);

        var response = await new UnarchiveConversationEndpoint(client).CallAsync(u1.UserId, u2.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UnarchiveConversation_WithoutAuth_Returns401()
    {
        var response = await new UnarchiveConversationEndpoint(HttpClient).CallAsync(1, 2);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── MuteConversation ──────────────────────────────────────────────────────

    [Fact]
    public async Task MuteConversation_AsOwner_Returns200()
    {
        var u1     = await Data.CreateUserAsync("msg-mute-u1@e2e.com");
        var u2     = await Data.CreateUserAsync("msg-mute-u2@e2e.com");
        var client = CreateAuthenticatedClient(u1.UserId, u1.UserGuid);

        var response = await new MuteConversationEndpoint(client).CallAsync(u1.UserId, u2.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MuteConversation_AsOtherUser_Returns403()
    {
        var u1       = await Data.CreateUserAsync("msg-mute-own@e2e.com");
        var u2       = await Data.CreateUserAsync("msg-mute-other@e2e.com");
        var intruder = await Data.CreateUserAsync("msg-mute-int@e2e.com");
        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);

        var response = await new MuteConversationEndpoint(client).CallAsync(u1.UserId, u2.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task MuteConversation_WithoutAuth_Returns401()
    {
        var response = await new MuteConversationEndpoint(HttpClient).CallAsync(1, 2);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── UnmuteConversation ────────────────────────────────────────────────────

    [Fact]
    public async Task UnmuteConversation_AsOwner_Returns200()
    {
        var u1     = await Data.CreateUserAsync("msg-unmute-u1@e2e.com");
        var u2     = await Data.CreateUserAsync("msg-unmute-u2@e2e.com");
        var client = CreateAuthenticatedClient(u1.UserId, u1.UserGuid);

        // Mute first
        await new MuteConversationEndpoint(client).CallAsync(u1.UserId, u2.UserId);

        var response = await new UnmuteConversationEndpoint(client).CallAsync(u1.UserId, u2.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UnmuteConversation_AsOtherUser_Returns403()
    {
        var u1       = await Data.CreateUserAsync("msg-unmute-own@e2e.com");
        var u2       = await Data.CreateUserAsync("msg-unmute-other@e2e.com");
        var intruder = await Data.CreateUserAsync("msg-unmute-int@e2e.com");
        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);

        var response = await new UnmuteConversationEndpoint(client).CallAsync(u1.UserId, u2.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UnmuteConversation_WithoutAuth_Returns401()
    {
        var response = await new UnmuteConversationEndpoint(HttpClient).CallAsync(1, 2);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── UnblockUser ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UnblockUser_AsOwner_Returns200()
    {
        var u1     = await Data.CreateUserAsync("msg-unblk-u1@e2e.com");
        var u2     = await Data.CreateUserAsync("msg-unblk-u2@e2e.com");
        var client = CreateAuthenticatedClient(u1.UserId, u1.UserGuid);

        // Block first
        await new BlockUserEndpoint(client).CallAsync(u1.UserId, u2.UserId);

        var response = await new UnblockUserEndpoint(client).CallAsync(u1.UserId, u2.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UnblockUser_AsOtherUser_Returns403()
    {
        var u1       = await Data.CreateUserAsync("msg-unblk-own@e2e.com");
        var u2       = await Data.CreateUserAsync("msg-unblk-other@e2e.com");
        var intruder = await Data.CreateUserAsync("msg-unblk-int@e2e.com");
        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);

        var response = await new UnblockUserEndpoint(client).CallAsync(u1.UserId, u2.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UnblockUser_WithoutAuth_Returns401()
    {
        var response = await new UnblockUserEndpoint(HttpClient).CallAsync(1, 2);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── SearchMessages ────────────────────────────────────────────────────────

    [Fact]
    public async Task SearchMessages_AsAuthenticatedUser_Returns200WithResults()
    {
        var u1     = await Data.CreateUserAsync("msg-srch-u1@e2e.com");
        var u2     = await Data.CreateUserAsync("msg-srch-u2@e2e.com");
        await Data.CreateMessageAsync(u1.UserId, u2.UserId, "Searchable unique string xyz");
        var client = CreateAuthenticatedClient(u1.UserId, u1.UserGuid);

        var response = await new SearchMessagesEndpoint(client).CallAsync("unique string xyz");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SearchMessages_EmptyQuery_Returns200OrBadRequest()
    {
        var user   = await Data.CreateUserAsync("msg-srch-empty@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new SearchMessagesEndpoint(client).CallAsync("");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SearchMessages_WithoutAuth_Returns401()
    {
        var response = await new SearchMessagesEndpoint(HttpClient).CallAsync("test");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── ReportAbuse ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ReportAbuse_AsReporter_Returns200()
    {
        var reporter = await Data.CreateUserAsync("msg-rpt-u1@e2e.com");
        var reported = await Data.CreateUserAsync("msg-rpt-u2@e2e.com");
        var msg      = await Data.CreateMessageAsync(reported.UserId, reporter.UserId, "Abusive content");
        var client   = CreateAuthenticatedClient(reporter.UserId, reporter.UserGuid);

        var response = await new ReportAbuseEndpoint(client)
            .CallAsync(reporter.UserId, reported.UserId, msg.MessageId, "Harassment");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReportAbuse_AsOtherUser_Returns403()
    {
        var u1       = await Data.CreateUserAsync("msg-rpt-own@e2e.com");
        var u2       = await Data.CreateUserAsync("msg-rpt-other@e2e.com");
        var intruder = await Data.CreateUserAsync("msg-rpt-int@e2e.com");
        var msg      = await Data.CreateMessageAsync(u2.UserId, u1.UserId, "Content");
        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);

        // Trying to report as u1 (not the authenticated intruder)
        var response = await new ReportAbuseEndpoint(client)
            .CallAsync(u1.UserId, u2.UserId, msg.MessageId, "Spam");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReportAbuse_WithoutAuth_Returns401()
    {
        var response = await new ReportAbuseEndpoint(HttpClient).CallAsync(1, 2, 1, "Spam");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
