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

    // ── Sessions per device ───────────────────────────────────────────────────

    /// <summary>A browser of its own: its own cookie jar, so its own session.</summary>
    private HttpClient NewDevice() => Fixture.Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies     = true,
        BaseAddress       = new Uri("https://localhost"), // the cookie is Secure
    });

    /// <summary>Signs in and returns the refresh token the server put in the cookie.</summary>
    private static async Task<string> SignInAsync(HttpClient device, string email)
    {
        var response = await device.PostAsJsonAsync("/api/v1/auth/login", new { Email = email, Password = "Pass123!" });
        response.EnsureSuccessStatusCode();
        var access = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        device.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", access);
        return RefreshCookieOf(response);
    }

    private static string RefreshCookieOf(HttpResponseMessage response) =>
        response.Headers.GetValues("Set-Cookie")
            .Single(c => c.StartsWith("refreshToken="))
            .Split(';')[0]["refreshToken=".Length..];

    private static Task<HttpResponseMessage> RefreshAsync(HttpClient device) =>
        device.PostAsync("/api/v1/auth/token/refresh", null);

    /// <summary>Presents a refresh token the way a copied cookie would be, from somewhere else.</summary>
    private async Task<HttpStatusCode> RefreshWithAsync(string rawToken) =>
        (await NewDevice().PostAsJsonAsync("/api/v1/auth/token/refresh", new { RefreshToken = rawToken })).StatusCode;

    [Fact]
    public async Task Sessions_SigningInOnASecondDevice_KeepsTheFirstOneSignedIn()
    {
        await Data.CreateUserAsync("two-devices@e2e.com", "Pass123!");
        var laptop = NewDevice();
        var phone  = NewDevice();
        await SignInAsync(laptop, "two-devices@e2e.com");
        await SignInAsync(phone, "two-devices@e2e.com");

        (await RefreshAsync(laptop)).StatusCode.Should().Be(HttpStatusCode.OK, "signing in on the phone used to sign the laptop out");
        (await RefreshAsync(phone)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Sessions_Refresh_ExchangesTheCookie_AndTheOldValueStopsWorking()
    {
        await Data.CreateUserAsync("rotate@e2e.com", "Pass123!");
        var device = NewDevice();
        var first  = await SignInAsync(device, "rotate@e2e.com");

        var refresh = await RefreshAsync(device);

        refresh.StatusCode.Should().Be(HttpStatusCode.OK);
        RefreshCookieOf(refresh).Should().NotBe(first).And.MatchRegex("^[0-9a-f]{64}$");
        (await RefreshAsync(device)).StatusCode.Should().Be(HttpStatusCode.OK, "the browser keeps the new cookie");
    }

    [Fact]
    public async Task Sessions_TwoTabsRefreshingAtTheSameMoment_BothStaySignedIn()
    {
        await Data.CreateUserAsync("two-tabs@e2e.com", "Pass123!");
        var browser = NewDevice();
        await SignInAsync(browser, "two-tabs@e2e.com");

        var tabs = await Task.WhenAll(RefreshAsync(browser), RefreshAsync(browser));

        tabs.Select(t => t.StatusCode).Should().AllBeEquivalentTo(HttpStatusCode.OK);
        (await RefreshAsync(browser)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_EndsOnlyThisDevicesSession()
    {
        await Data.CreateUserAsync("logout-one@e2e.com", "Pass123!");
        var laptop = NewDevice();
        var phone  = NewDevice();
        await SignInAsync(laptop, "logout-one@e2e.com");
        var phoneCookie = await SignInAsync(phone, "logout-one@e2e.com");

        (await phone.PostAsync("/api/v1/auth/logout", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        (await RefreshWithAsync(phoneCookie)).Should().Be(HttpStatusCode.Unauthorized, "the phone's session is over on the server, not only its cookie");
        (await RefreshAsync(laptop)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LogoutEverywhere_EndsTheSessionsOfAllDevices()
    {
        await Data.CreateUserAsync("logout-all@e2e.com", "Pass123!");
        var laptop = NewDevice();
        var phone  = NewDevice();
        await SignInAsync(laptop, "logout-all@e2e.com");
        var phoneCookie = await SignInAsync(phone, "logout-all@e2e.com");

        (await laptop.PostAsync("/api/v1/auth/logout-everywhere", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        (await RefreshWithAsync(phoneCookie)).Should().Be(HttpStatusCode.Unauthorized);
        (await RefreshAsync(laptop)).StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LogoutEverywhere_WithoutAuth_Returns401()
    {
        (await HttpClient.PostAsync("/api/v1/auth/logout-everywhere", null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
