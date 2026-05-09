using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LandlordApp.Tests.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace LandlordApp.Tests.IntegrationTests.Auth;

/// <summary>
/// Covers the HTTP auth pipeline end-to-end: login, token usage, refresh, logout.
/// Uses a real in-memory app server via WebApplicationFactory.
///
/// NOTE: Tests that exercise the "wrong password with existing user" path are
/// excluded because AuthService uses ExecuteSqlRawAsync for the failed-login counter,
/// which is not supported by the InMemory provider.
/// </summary>
public class AuthFlowTests : IntegrationTestBase, IClassFixture<LanderWebApplicationFactory>
{
    public AuthFlowTests(LanderWebApplicationFactory factory) : base(factory) { }

    // ── Login ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithValidCredentials_Returns200AndAccessToken()
    {
        await SeedUserAsync("login-valid@test.com", "Password123!");
        var client = CreateAnonymousClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email    = "login-valid@test.com",
            Password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_Returns401()
    {
        var client = CreateAnonymousClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email    = "nobody@nowhere.com",
            Password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithInactiveUser_Returns403WithEmailNotVerifiedMessage()
    {
        // Inactive user = IsActive false = email not verified in this app's model
        using var scope = CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<Lander.UsersContext>();
        ctx.Users.Add(new Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate.User
        {
            FirstName = "Inactive",
            LastName  = "User",
            Email     = "inactive@test.com",
            Password  = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            UserGuid  = Guid.NewGuid(),
            IsActive  = false,
        });
        await ctx.SaveChangesAsync();

        var client = CreateAnonymousClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email    = "inactive@test.com",
            Password = "Password123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().Be("EMAIL_NOT_VERIFIED");
    }

    // ── Protected endpoints ──────────────────────────────────────────────────

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var client = CreateAnonymousClient();
        var response = await client.GetAsync("/api/v1/auth/profile/1");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_Returns200OrNotFound()
    {
        var user = await SeedUserAsync("protected@test.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await client.GetAsync($"/api/v1/auth/profile/{user.UserId}");

        // 200 = found own profile; 404 would mean a bug in seeding
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetOtherUserProfile_ReturnsSanitizedPii()
    {
        var owner   = await SeedUserAsync("owner-profile@test.com");
        var viewer  = await SeedUserAsync("viewer-profile@test.com");
        var client  = CreateAuthenticatedClient(viewer.UserId, viewer.UserGuid);

        var response = await client.GetAsync($"/api/v1/auth/profile/{owner.UserId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        // PII fields must be empty for other users' profiles
        body.GetProperty("email").GetString().Should().BeNullOrEmpty();
    }

    // ── Token refresh ────────────────────────────────────────────────────────

    [Fact]
    public async Task TokenRefresh_WithoutCookie_Returns400()
    {
        var client   = CreateAnonymousClient();
        var response = await client.PostAsync("/api/v1/auth/token/refresh", null);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task FullAuthFlow_Login_Refresh_Logout_WorksEndToEnd()
    {
        await SeedUserAsync("fullflow@test.com", "Password123!");
        var client = CreateAnonymousClient();

        // 1. Login → should set refreshToken cookie and return access token
        var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email    = "fullflow@test.com",
            Password = "Password123!"
        });
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginBody   = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = loginBody.GetProperty("accessToken").GetString();
        accessToken.Should().NotBeNullOrEmpty();

        // Cookie is handled by the client's CookieContainer (HandleCookies = true)

        // 2. Refresh token → should return a new access token using the httpOnly cookie
        var refreshResp = await client.PostAsync("/api/v1/auth/token/refresh", null);
        refreshResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshBody = await refreshResp.Content.ReadFromJsonAsync<JsonElement>();
        refreshBody.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();

        // 3. Logout → should clear cookie and return 200
        var logoutResp = await client.PostAsync("/api/v1/auth/logout", null);
        logoutResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Rate limiting ────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ExceedingRateLimit_Returns429()
    {
        var client = CreateAnonymousClient();

        // Auth rate limit is 5 per 30 seconds — fire 6 identical requests
        HttpResponseMessage? lastResponse = null;
        for (int i = 0; i < 6; i++)
        {
            lastResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
            {
                Email    = $"ratelimit-{i}@test.com",
                Password = "WrongPassword"
            });
        }

        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
