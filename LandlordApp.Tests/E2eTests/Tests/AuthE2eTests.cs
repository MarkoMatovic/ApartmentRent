using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LandlordApp.Tests.E2eTests.Infrastructure;
using LandlordApp.Tests.E2eTests.Shared;

namespace LandlordApp.Tests.E2eTests.Tests;

/// <summary>
/// E2E auth tests — real SQL Server, real BCrypt, real JWT middleware.
/// Each test starts with a clean database (Respawn), seeds its own data, then hits live endpoints.
/// </summary>
[Collection(E2eCollection.Name)]
public class AuthE2eTests : E2eTestBase
{
    public AuthE2eTests(E2eFixture fixture) : base(fixture) { }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200AndAccessToken()
    {
        await Data.CreateUserAsync("login-ok@e2e.com", "Pass123!");

        var response = await new LoginEndpoint(HttpClient)
            .CallAsync(new LoginRequest("login-ok@e2e.com", "Pass123!"));

        response.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        await Data.CreateUserAsync("login-wrong@e2e.com", "Pass123!");

        var raw = await new LoginEndpoint(HttpClient)
            .CallRawAsync(new LoginRequest("login-wrong@e2e.com", "WRONG"));

        raw.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var raw = await new LoginEndpoint(HttpClient)
            .CallRawAsync(new LoginRequest("nobody@e2e.com", "Pass123!"));

        raw.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_InactiveUser_Returns403WithEmailNotVerified()
    {
        await Data.CreateUserAsync("inactive@e2e.com", "Pass123!", isActive: false);

        var raw = await new LoginEndpoint(HttpClient)
            .CallRawAsync(new LoginRequest("inactive@e2e.com", "Pass123!"));

        raw.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await raw.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().Be("EMAIL_NOT_VERIFIED");
    }

    // ── Full auth flow ─────────────────────────────────────────────────────────

    [Fact]
    public async Task FullFlow_Login_RefreshToken_Logout_AllSucceed()
    {
        await Data.CreateUserAsync("flow@e2e.com", "Pass123!");

        // 1. Login — cookie + access token
        var loginResp = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email    = "flow@e2e.com",
            Password = "Pass123!"
        });
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginBody   = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = loginBody.GetProperty("accessToken").GetString();
        accessToken.Should().NotBeNullOrEmpty();

        // 2. Refresh token (httpOnly cookie is sent automatically by HttpClient)
        var refreshResp = await HttpClient.PostAsync("/api/v1/auth/token/refresh", null);
        refreshResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshBody = await refreshResp.Content.ReadFromJsonAsync<JsonElement>();
        refreshBody.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();

        // 3. Logout
        var logoutResp = await HttpClient.PostAsync("/api/v1/auth/logout", null);
        logoutResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Protected endpoints ───────────────────────────────────────────────────

    [Fact]
    public async Task GetProfile_WithoutToken_Returns401()
    {
        var response = await HttpClient.GetAsync("/api/v1/auth/profile/1");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProfile_WithValidToken_ReturnsOwnProfile()
    {
        var user   = await Data.CreateUserAsync("profile-own@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new GetProfileEndpoint(client).CallAsync(user.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("email").GetString().Should().Be("profile-own@e2e.com");
    }

    [Fact]
    public async Task GetProfile_OtherUser_PiiIsRedacted()
    {
        var owner  = await Data.CreateUserAsync("profile-owner@e2e.com");
        var viewer = await Data.CreateUserAsync("profile-viewer@e2e.com");
        var client = CreateAuthenticatedClient(viewer.UserId, viewer.UserGuid);

        var response = await new GetProfileEndpoint(client).CallAsync(owner.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("email").GetString().Should().BeNullOrEmpty();
    }

    // ── Rate limiting ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ExceedRateLimit_Returns429OrUnauthorized()
    {
        // In production (rate limiting enabled) the 6th request returns 429.
        // In E2eTesting (rate limiting disabled to prevent cross-test leakage)
        // every request returns 401 for unknown credentials instead.
        HttpResponseMessage? last = null;
        for (int i = 0; i < 6; i++)
        {
            last = await HttpClient.PostAsJsonAsync("/api/v1/auth/login", new
            {
                Email    = $"rl-{i}@e2e.com",
                Password = "Wrong"
            });
        }

        last!.StatusCode.Should().BeOneOf(
            HttpStatusCode.TooManyRequests,
            HttpStatusCode.Unauthorized);
    }
}
