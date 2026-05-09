using Lander;
using Lander.src.Modules.Communication.Interfaces;
using Microsoft.Extensions.Configuration;
using Lander.src.Modules.Reviews.Client;
using Lander.src.Modules.Reviews.proto;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using LandlordApp.Tests.IntegrationTests.Infrastructure;
using Moq;

namespace LandlordApp.Tests.E2eTests.Infrastructure;

/// <summary>
/// Full ASP.NET Core host running against a real SQL Server container.
/// External integrations (email, gRPC) are replaced with Moq mocks so that:
///   - tests can verify calls  (e.g. mock.Verify(…))
///   - side-effects are eliminated (no real emails, no gRPC calls)
///
/// Connection string is provided by <see cref="E2eFixture"/> after the container starts.
/// </summary>
public class E2eWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    // ── Exposed mocks (tests can Setup / Verify on these) ────────────────────
    public Mock<IEmailService>     MockEmail  { get; } = new();
    public Mock<IGrpcServiceClient> MockGrpc  { get; } = new();

    public E2eWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("E2eTesting");

        // ── Override configuration ────────────────────────────────────────────
        builder.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Use the Testcontainers SQL Server for ALL 12 DbContexts
                ["ConnectionStrings:DefaultConnection"]    = _connectionString,
                // Use the same JWT secret as TestJwtGenerator so auth tests work
                ["Jwt:Secret"]                             = TestJwtGenerator.TestSecret,
                ["Jwt:Issuer"]                             = TestJwtGenerator.TestIssuer,
                ["Jwt:Audience"]                           = TestJwtGenerator.TestAudience,
                // Stub values for external services (never actually called)
                ["Brevo:ApiKey"]                           = "e2e-brevo-key",
                ["Monri:AuthenticityToken"]                = "e2e-auth-token",
                ["Monri:MerchantKey"]                      = "e2e-merchant-key",
                // Minimal plan so CreatePayment tests can resolve "basic" without 400
                ["Monri:Plans:basic:Name"]                 = "Basic Plan",
                ["Monri:Plans:basic:Amount"]               = "999",
                ["Monri:Plans:basic:Currency"]             = "EUR",
                ["Monri:Plans:premium:Name"]               = "Premium Plan",
                ["Monri:Plans:premium:Amount"]             = "1999",
                ["Monri:Plans:premium:Currency"]           = "EUR",
                ["GrpcServerUrl"]                          = "http://localhost:9999",
                ["Redis:Configuration"]                    = "",   // skip Redis
                ["Security:MaxFailedLoginAttempts"]        = "5",
                ["Security:LockoutMinutes"]                = "15",
            }));

        builder.ConfigureServices(services =>
        {
            // ── No background services (avoid real scheduler / SignalR hubs) ──
            services.RemoveAll<IHostedService>();

            // ── Replace external integrations with mocks ──────────────────────
            services.RemoveAll<IEmailService>();
            services.AddScoped<IEmailService>(_ => MockEmail.Object);

            services.RemoveAll<IGrpcServiceClient>();
            services.AddScoped<IGrpcServiceClient>(_ => MockGrpc.Object);

            // Default NoopGrpcClient behaviour so tests that don't care don't fail
            SetupDefaultGrpcMockBehavior();

            // ── JWT: test server is HTTP-only ─────────────────────────────────
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                opts => opts.RequireHttpsMetadata = false);
        });
    }

    // ── Mock reset (called by E2eFixture.ResetStateAsync before each test) ───

    /// <summary>Resets all mock state so tests don't bleed into each other.</summary>
    public void ResetMocks()
    {
        MockEmail.Reset();
        MockGrpc.Reset();
        SetupDefaultGrpcMockBehavior();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetupDefaultGrpcMockBehavior()
    {
        MockGrpc.Setup(g => g.CreateFavoriteAsync(It.IsAny<CreateFavoriteRequest>()))
                .ReturnsAsync(new FavoriteResponse());
        MockGrpc.Setup(g => g.CreateReviewAsync(It.IsAny<CreateReviewRequest>()))
                .ReturnsAsync(new ReviewResponse());
        MockGrpc.Setup(g => g.GetReviewByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new ReviewResponse());
        MockGrpc.Setup(g => g.GetReviewsByApartmentIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new GetReviewsResponse());
        MockGrpc.Setup(g => g.DeleteReviewAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new DeleteResponse());
        MockGrpc.Setup(g => g.DeleteFavoriteAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new DeleteResponse());
        MockGrpc.Setup(g => g.GetUserFavoritesAsync(It.IsAny<int>()))
                .ReturnsAsync(new GetFavoritesResponse());
    }
}
