using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LandlordApp.Tests.E2eTests.Infrastructure;
using LandlordApp.Tests.E2eTests.Shared;

namespace LandlordApp.Tests.E2eTests.Tests;

/// <summary>
/// E2E tests for user endpoints not covered in UserRegistrationE2eTests:
///   – ForgotPassword / ResetPassword (AllowAnonymous)
///   – VerifyEmail (AllowAnonymous)
///   – ExportUserData (Authorize + ownership)
///   – UpdateRoommateStatus (Authorize + ownership)
///   – ReactivateUser (Admin only)
/// </summary>
[Collection(E2eCollection.Name)]
public class UserExtrasE2eTests : E2eTestBase
{
    public UserExtrasE2eTests(E2eFixture fixture) : base(fixture) { }

    // ── ForgotPassword [AllowAnonymous] ───────────────────────────────────────

    [Fact]
    public async Task ForgotPassword_WithExistingEmail_Returns200()
    {
        var user = await Data.CreateUserAsync("usr-fp@e2e.com");

        var response = await new ForgotPasswordEndpoint(HttpClient).CallAsync(user.Email);

        // Always returns 200 (doesn't reveal if email exists) — intentional UX
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_WithNonExistentEmail_Returns200()
    {
        var response = await new ForgotPasswordEndpoint(HttpClient).CallAsync("doesnotexist@e2e.com");

        // Same 200 to prevent email enumeration
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_WithInvalidEmailFormat_Returns400OrOk()
    {
        var response = await new ForgotPasswordEndpoint(HttpClient).CallAsync("not-an-email");

        // May validate format (400) or silently accept (200) — both acceptable
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    // ── ResetPassword [AllowAnonymous] ────────────────────────────────────────

    [Fact]
    public async Task ResetPassword_WithInvalidToken_Returns400()
    {
        var response = await new ResetPasswordEndpoint(HttpClient).CallAsync(
            token: "invalid-token-that-does-not-exist",
            newPassword: "NewPassword123!");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_WithEmptyToken_Returns400()
    {
        var response = await new ResetPasswordEndpoint(HttpClient).CallAsync(
            token: "",
            newPassword: "NewPassword123!");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── VerifyEmail [AllowAnonymous] ──────────────────────────────────────────

    [Fact]
    public async Task VerifyEmail_WithInvalidToken_Returns400()
    {
        var response = await new VerifyEmailEndpoint(HttpClient).CallAsync("invalid-verification-token");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task VerifyEmail_WithEmptyToken_Returns400()
    {
        var response = await new VerifyEmailEndpoint(HttpClient).CallAsync("");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── ExportUserData [Authorize + ownership] ────────────────────────────────

    [Fact]
    public async Task ExportUserData_AsOwner_Returns200WithData()
    {
        var user   = await Data.CreateUserAsync("usr-exp@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new ExportUserDataEndpoint(client).CallAsync(user.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().NotBe(JsonValueKind.Undefined);
    }

    [Fact]
    public async Task ExportUserData_AsOtherUser_Returns403()
    {
        var owner    = await Data.CreateUserAsync("usr-exp-own@e2e.com");
        var intruder = await Data.CreateUserAsync("usr-exp-int@e2e.com");
        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);

        var response = await new ExportUserDataEndpoint(client).CallAsync(owner.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ExportUserData_AsAdmin_Returns200()
    {
        var owner  = await Data.CreateUserAsync("usr-exp-adm-target@e2e.com");
        var admin  = await Data.CreateUserAsync("usr-exp-adm@e2e.com", role: "Admin");
        var client = CreateAuthenticatedClient(admin.UserId, admin.UserGuid, role: "Admin");

        var response = await new ExportUserDataEndpoint(client).CallAsync(owner.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ExportUserData_WithoutAuth_Returns401()
    {
        var user = await Data.CreateUserAsync("usr-exp-na@e2e.com");

        var response = await new ExportUserDataEndpoint(HttpClient).CallAsync(user.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── UpdateRoommateStatus [Authorize + ownership] ──────────────────────────

    [Fact]
    public async Task UpdateRoommateStatus_AsOwner_Returns200()
    {
        var user   = await Data.CreateUserAsync("usr-rmst-own@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new UpdateRoommateStatusEndpoint(client).CallAsync(user.UserGuid, isActive: true);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateRoommateStatus_AsOtherUser_Returns403()
    {
        var owner    = await Data.CreateUserAsync("usr-rmst-target@e2e.com");
        var intruder = await Data.CreateUserAsync("usr-rmst-int@e2e.com");
        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);

        var response = await new UpdateRoommateStatusEndpoint(client).CallAsync(owner.UserGuid, isActive: false);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateRoommateStatus_WithoutAuth_Returns401()
    {
        var response = await new UpdateRoommateStatusEndpoint(HttpClient).CallAsync(Guid.NewGuid(), isActive: true);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── ReactivateUser [Admin only] ───────────────────────────────────────────

    [Fact]
    public async Task ReactivateUser_AsAdmin_Returns200()
    {
        var target = await Data.CreateUserAsync("usr-react-target@e2e.com", isActive: false);
        var admin  = await Data.CreateUserAsync("usr-react-adm@e2e.com", role: "Admin");
        var client = CreateAuthenticatedClient(admin.UserId, admin.UserGuid, role: "Admin");

        var response = await new ReactivateUserEndpoint(client).CallAsync(target.UserGuid);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReactivateUser_AsRegularUser_Returns403()
    {
        var target = await Data.CreateUserAsync("usr-react-t2@e2e.com");
        var user   = await Data.CreateUserAsync("usr-react-reg@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new ReactivateUserEndpoint(client).CallAsync(target.UserGuid);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReactivateUser_WithoutAuth_Returns401()
    {
        var response = await new ReactivateUserEndpoint(HttpClient).CallAsync(Guid.NewGuid());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
