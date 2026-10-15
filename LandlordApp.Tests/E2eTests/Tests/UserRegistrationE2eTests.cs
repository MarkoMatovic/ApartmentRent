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

    // The 400 body carries a stable validation code per field (not an English sentence) so the
    // frontend can translate it.

    [Theory]
    [InlineData("Ab1", "PASSWORD_TOO_SHORT")]
    [InlineData("password1", "PASSWORD_NEEDS_UPPER")]
    [InlineData("PASSWORD1", "PASSWORD_NEEDS_LOWER")]
    [InlineData("Passwordd", "PASSWORD_NEEDS_DIGIT")]
    public async Task Register_WeakPassword_ReturnsTheBrokenRuleAsACode(string password, string expectedCode)
    {
        var response = await new RegisterWithDetailsEndpoint(HttpClient)
            .CallAsync("reg-code@e2e.com", password);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").GetProperty("Password")[0].GetString().Should().Be(expectedCode);
    }

    [Fact]
    public async Task Register_InvalidEmail_ReturnsEmailInvalidCode()
    {
        var response = await new RegisterWithDetailsEndpoint(HttpClient)
            .CallAsync("not-an-email", "Password123!");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").GetProperty("Email")[0].GetString().Should().Be("EMAIL_INVALID");
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
    public async Task UpdateProfile_WithPicture_StoresItInFileStorageAndReturnsItsAddress()
    {
        var user   = await Data.CreateUserAsync("upd-pic@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new UpdateProfileEndpoint(client).CallAsync(user.UserId, new
        {
            ProfilePicture = LandlordApp.Tests.Services.ProfilePictureServiceTests.PhotoDataUrl(),
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var picture = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("profilePicture").GetString();
        picture.Should().StartWith($"https://storage.test/avatars/{user.UserGuid:N}/").And.EndWith(".webp");
        Fixture.Factory.Storage.Files.Should().ContainSingle();

        // The profile read afterwards carries the same short address.
        var profile = await client.GetFromJsonAsync<JsonElement>($"/api/v1/auth/profile/{user.UserId}");
        profile.GetProperty("profilePicture").GetString().Should().Be(picture);
    }

    [Fact]
    public async Task UpdateProfile_WithSomethingThatIsNotAPicture_Returns422WithCode()
    {
        var user   = await Data.CreateUserAsync("upd-badpic@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new UpdateProfileEndpoint(client).CallAsync(user.UserId, new
        {
            ProfilePicture = "https://tracker.example/pixel.png",
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString()
            .Should().Be("INVALID_PROFILE_PICTURE");
        Fixture.Factory.Storage.Files.Should().BeEmpty();
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
    public async Task ChangePassword_WithWrongOldPassword_Returns422WithBusinessCode()
    {
        // A wrong current password is the caller's mistake, not a server fault (it used to be a 500).
        var user   = await Data.CreateUserAsync("chpwd-code@e2e.com", password: "Correct123!");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new ChangePasswordEndpoint(client)
            .CallAsync(user.UserGuid, "WrongPassword1!", "NewPassword456!");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetString().Should().Be("INCORRECT_OLD_PASSWORD");
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
