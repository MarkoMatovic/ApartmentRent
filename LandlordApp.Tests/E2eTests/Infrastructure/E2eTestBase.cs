using System.Net.Http.Headers;
using System.Net.Http.Json;
using Lander;
using Lander.src.Modules.Communication.Models;
using Lander.src.Modules.Listings.Models;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using LandlordApp.Tests.IntegrationTests.Infrastructure;

namespace LandlordApp.Tests.E2eTests.Infrastructure;

/// <summary>
/// Base class for all E2E tests. Inheriting classes must be decorated with
/// <c>[Collection(E2eCollection.Name)]</c> so xUnit injects <see cref="E2eFixture"/>.
///
/// Before every test (<see cref="InitializeAsync"/>):
///   1. DB is wiped via Respawn (lookup tables preserved)
///   2. All mocks are reset to their default state
///
/// Provides:
///   - <see cref="HttpClient"/> — anonymous client
///   - <see cref="CreateAuthenticatedClient"/> — client with pre-issued JWT
///   - <see cref="Data"/>       — TestDataFactory for seeding entities
///   - <see cref="Scope"/>      — DI scope for direct DB access
/// </summary>
public abstract class E2eTestBase : IAsyncLifetime
{
    protected readonly E2eFixture Fixture;

    // Filled in InitializeAsync
    protected HttpClient HttpClient { get; private set; } = null!;
    protected TestDataFactory Data   { get; private set; } = null!;
    protected IServiceScope Scope    { get; private set; } = null!;

    protected E2eTestBase(E2eFixture fixture)
    {
        Fixture = fixture;
    }

    // ── IAsyncLifetime ────────────────────────────────────────────────────────

    public virtual async Task InitializeAsync()
    {
        // 1. Wipe DB rows + reset mocks
        await Fixture.ResetStateAsync();

        // 2. Fresh DI scope for this test
        Scope      = Fixture.Factory.Services.CreateScope();
        HttpClient = Fixture.Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies     = true,
        });

        Data = new TestDataFactory(Scope.ServiceProvider);
    }

    public virtual Task DisposeAsync()
    {
        Scope.Dispose();
        HttpClient.Dispose();
        return Task.CompletedTask;
    }

    // ── Client helpers ────────────────────────────────────────────────────────

    /// <summary>Creates an HttpClient that carries a pre-issued JWT for the given user.</summary>
    protected HttpClient CreateAuthenticatedClient(int userId, Guid userGuid, string role = "Tenant")
    {
        var client = Fixture.Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies     = true,
        });
        var token = TestJwtGenerator.Generate(userId, userGuid, role);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>Performs a real login and returns an HttpClient that carries the access token.</summary>
    protected async Task<HttpClient> LoginAsync(string email, string password)
    {
        var loginClient = Fixture.Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies     = true,
        });
        var response = await loginClient.PostAsJsonAsync("/api/v1/auth/login", new { Email = email, Password = password });
        response.EnsureSuccessStatusCode();

        var body  = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var token = body.GetProperty("accessToken").GetString()!;
        loginClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return loginClient;
    }
}
