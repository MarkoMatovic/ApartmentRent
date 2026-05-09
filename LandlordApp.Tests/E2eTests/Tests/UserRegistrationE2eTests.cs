using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LandlordApp.Tests.E2eTests.Infrastructure;
using LandlordApp.Tests.E2eTests.Shared;

namespace LandlordApp.Tests.E2eTests.Tests;

/// <summary>
/// E2E tests for user registration, profile, and account management endpoints.
/// </summary>
[Collection(E2eCollection.Name)]
public class UserRegistrationE2eTests : E2eTestBase
{
    public UserRegistrationE2eTests(E2eFixture fixture) : base(fixture) { }

    // ── Register ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_WithValidData_Returns200()
    {
        var response = await new RegisterWithDetailsEndpoint(HttpClient)
            .CallAsync("reg-new@e2e.com", "Password123!");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("email").GetString().Should().Be("reg-new@e2e.com");
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns4xx()
    {
        // Seed a user with the same email directly
        await Data.CreateUserAsync("reg-dup@e2e.com");

        var response = await new RegisterWithDetailsEndpoint(HttpClient)
            .CallAsync("reg-dup@e2e.com", "Password123!");

        // Conflict or BadRequest depending on service implementation
        ((int)response.StatusCode).Should().BeGreaterThanOrEqualTo(400);
    }

    [Fact]
    public async Task Register_WeakPassword_Returns400()
    {
        var response = await new RegisterWithDetailsEndpoint(HttpClient)
            .CallAsync("reg-weak@e2e.com", "123");  // too short / no uppercase

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GetUserProfile ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserProfile_AsOwner_Returns200WithFullData()
    {
        var user   = await Data.CreateUserAsync("profile-own@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new GetUserProfileEndpoint(client).CallAsync(user.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("email").GetString().Should().Be("profile-own@e2e.com");
    }

    [Fact]
    public async Task GetUserProfile_AsOtherUser_Returns200WithSanitizedData()
    {
        var owner    = await Data.CreateUserAsync("profile-other-own@e2e.com");
        var caller   = await Data.CreateUserAsync("profile-other-cal@e2e.com");
        var client   = CreateAuthenticatedClient(caller.UserId, caller.UserGuid);

        var response = await new GetUserProfileEndpoint(client).CallAsync(owner.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        // PII should be stripped for other users
        body.GetProperty("email").GetString().Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task GetUserProfile_NonExistent_Returns404()
    {
        var user   = await Data.CreateUserAsync("profile-404@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new GetUserProfileEndpoint(client).CallAsync(999_999);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUserProfile_WithoutAuth_Returns401()
    {
        var user = await Data.CreateUserAsync("profile-na@e2e.com");

        var response = await new GetUserProfileEndpoint(HttpClient).CallAsync(user.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── UpdateProfile ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProfile_AsOwner_Returns200WithNewValues()
    {
        var user   = await Data.CreateUserAsync("upd-own@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new UpdateProfileEndpoint(client).CallAsync(user.UserId, new
        {
            FirstName   = "Novo",
            LastName    = "Ime",
            PhoneNumber = "+38761000000",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("firstName").GetString().Should().Be("Novo");
    }

    [Fact]
    public async Task UpdateProfile_AsOtherUser_Returns403()
    {
        var owner    = await Data.CreateUserAsync("upd-own2@e2e.com");
        var intruder = await Data.CreateUserAsync("upd-int@e2e.com");
        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);

        var response = await new UpdateProfileEndpoint(client).CallAsync(owner.UserId, new
        {
            FirstName = "Haker",
            LastName  = "Hak",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateProfile_WithoutAuth_Returns401()
    {
        var user = await Data.CreateUserAsync("upd-na@e2e.com");

        var response = await new UpdateProfileEndpoint(HttpClient).CallAsync(user.UserId, new
        {
            FirstName = "Bez",
            LastName  = "Autentikacije",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── UpdatePrivacySettings ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePrivacySettings_AsOwner_Returns200()
    {
        var user   = await Data.CreateUserAsync("priv-own@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new UpdatePrivacySettingsEndpoint(client)
            .CallAsync(user.UserId, analytics: false, chat: true, visible: true);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdatePrivacySettings_AsOtherUser_Returns403()
    {
        var owner    = await Data.CreateUserAsync("priv-own2@e2e.com");
        var intruder = await Data.CreateUserAsync("priv-int@e2e.com");
        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);

        var response = await new UpdatePrivacySettingsEndpoint(client)
            .CallAsync(owner.UserId, analytics: false, chat: false, visible: false);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdatePrivacySettings_WithoutAuth_Returns401()
    {
        var user = await Data.CreateUserAsync("priv-na@e2e.com");

        var response = await new UpdatePrivacySettingsEndpoint(HttpClient)
            .CallAsync(user.UserId, analytics: false, chat: false, visible: false);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── ChangePassword ────────────────────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_WithCorrectOldPassword_Returns200()
    {
        const string oldPwd = "OldPassword123!";
        const string newPwd = "NewPassword456!";
        var user   = await Data.CreateUserAsync("chpwd@e2e.com", password: oldPwd);
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new ChangePasswordEndpoint(client)
            .CallAsync(user.UserGuid, oldPwd, newPwd);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_WithWrongOldPassword_Returns4xx()
    {
        var user   = await Data.CreateUserAsync("chpwd-wrong@e2e.com", password: "Correct123!");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new ChangePasswordEndpoint(client)
            .CallAsync(user.UserGuid, "WrongPassword!", "NewPassword456!");

        ((int)response.StatusCode).Should().BeGreaterThanOrEqualTo(400);
    }

    [Fact]
    public async Task ChangePassword_WithoutAuth_Returns401()
    {
        var user = await Data.CreateUserAsync("chpwd-na@e2e.com");

        var response = await new ChangePasswordEndpoint(HttpClient)
            .CallAsync(user.UserGuid, "OldPassword123!", "NewPassword456!");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── DeactivateUser ────────────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateUser_AsOwner_Returns200()
    {
        var user   = await Data.CreateUserAsync("deact-own@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new DeactivateUserEndpoint(client).CallAsync(user.UserGuid);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeactivateUser_AsOtherUser_Returns403()
    {
        var owner    = await Data.CreateUserAsync("deact-own2@e2e.com");
        var intruder = await Data.CreateUserAsync("deact-int@e2e.com");
        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);

        var response = await new DeactivateUserEndpoint(client).CallAsync(owner.UserGuid);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeactivateUser_WithoutAuth_Returns401()
    {
        var user = await Data.CreateUserAsync("deact-na@e2e.com");

        var response = await new DeactivateUserEndpoint(HttpClient).CallAsync(user.UserGuid);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── DeleteUser ────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteUser_AsOwner_Returns200()
    {
        var user   = await Data.CreateUserAsync("del-own@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new DeleteUserEndpoint(client).CallAsync(user.UserGuid);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteUser_AsOtherUser_Returns403()
    {
        var owner    = await Data.CreateUserAsync("del-own2@e2e.com");
        var intruder = await Data.CreateUserAsync("del-int@e2e.com");
        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);

        var response = await new DeleteUserEndpoint(client).CallAsync(owner.UserGuid);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteUser_WithoutAuth_Returns401()
    {
        var user = await Data.CreateUserAsync("del-na@e2e.com");

        var response = await new DeleteUserEndpoint(HttpClient).CallAsync(user.UserGuid);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
