using Lander;
using Lander.src.Modules.Communication.Interfaces;
using Microsoft.Extensions.Configuration;
using Lander.src.Modules.Reviews.Interfaces;
using Lander.src.Modules.Reviews.proto;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace LandlordApp.Tests.IntegrationTests.Infrastructure;

/// <summary>
/// Spins up the full ASP.NET Core pipeline against in-memory databases.
/// One instance per test class (via IClassFixture) gives each class its own DB.
/// </summary>
public class LanderWebApplicationFactory : WebApplicationFactory<Program>
{
    // Unique suffix keeps each factory's 12 InMemory databases isolated from one another
    private readonly string _dbSuffix = Guid.NewGuid().ToString("N")[..12];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"]                        = TestJwtGenerator.TestSecret,
                ["Jwt:Issuer"]                        = TestJwtGenerator.TestIssuer,
                ["Jwt:Audience"]                      = TestJwtGenerator.TestAudience,
                ["Brevo:ApiKey"]                      = "test-brevo-key",
                // BrevoSettings has [Required] members validated on startup
                // outside Development/E2eTesting environments
                ["Brevo:SenderEmail"]                 = "test@landlander.test",
                ["Brevo:SenderName"]                  = "Landlander Test",
                ["ConnectionStrings:DefaultConnection"] = "test-not-used",
                ["GrpcServerUrl"]                     = "http://localhost:9999",
                ["Redis:Configuration"]               = "",  // empty → skip Redis
                ["Security:MaxFailedLoginAttempts"]   = "5",
                ["Security:LockoutMinutes"]           = "15",
            }));

        builder.ConfigureServices(services =>
        {
            ReplaceAllDbContexts(services);

            // Background services would hit real external systems or need migrations — remove all
            services.RemoveAll<IHostedService>();

            ReplaceExternalServices(services);

            // JWT was configured with RequireHttpsMetadata = true; test server is HTTP-only
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                opts => opts.RequireHttpsMetadata = false);
        });
    }

    // ── DbContext replacement ────────────────────────────────────────────────

    private void ReplaceAllDbContexts(IServiceCollection services)
    {
        SwapToInMemory<UsersContext>(services);
        SwapToInMemory<ListingsContext>(services);
        SwapToInMemory<CommunicationsContext>(services);
        SwapToInMemory<ApplicationsContext>(services);
        SwapToInMemory<NotificationContext>(services);
        SwapToInMemory<ReviewsContext>(services);
        SwapToInMemory<RoommatesContext>(services);
        SwapToInMemory<SearchRequestsContext>(services);
        SwapToInMemory<SavedSearchesContext>(services);
        SwapToInMemory<AnalyticsContext>(services);
        SwapToInMemory<Lander.src.Modules.Appointments.AppointmentsContext>(services);
        SwapToInMemory<Lander.src.Modules.Payments.PaymentsContext>(services);
    }

    private void SwapToInMemory<TContext>(IServiceCollection services) where TContext : DbContext
    {
        // Remove the real (SQL Server) options descriptor; AddDbContext re-adds an InMemory one.
        // Since EF Core 8 the configuration action is registered separately as
        // IDbContextOptionsConfiguration<TContext> — it must be removed too, otherwise
        // both providers end up registered and the context refuses to initialize.
        var existing = services
            .Where(d => d.ServiceType == typeof(DbContextOptions<TContext>)
                     || d.ServiceType == typeof(Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<TContext>))
            .ToList();
        foreach (var d in existing) services.Remove(d);

        services.AddDbContext<TContext>(opts =>
            opts.UseInMemoryDatabase($"{typeof(TContext).Name}_{_dbSuffix}")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
    }

    // ── External service stubs ───────────────────────────────────────────────

    private static void ReplaceExternalServices(IServiceCollection services)
    {
        services.RemoveAll<IEmailService>();
        services.AddScoped<IEmailService, NoopEmailService>();

        services.RemoveAll<IReviewFavoriteService>();
        services.AddScoped<IReviewFavoriteService, NoopReviewFavoriteService>();
    }

    // ── Noop implementations ─────────────────────────────────────────────────

    private sealed class NoopEmailService : IEmailService
    {
        public Task<bool> SendEmailAsync(string to, string subject, string htmlContent) => Task.FromResult(true);
        public Task<bool> SendTemplatedEmailAsync(string to, string subject, string templateName, object templateData) => Task.FromResult(true);
        public Task<bool> SendBulkEmailAsync(List<string> recipients, string subject, string htmlContent) => Task.FromResult(true);
        public Task<bool> SendWelcomeEmailAsync(string to, string userName) => Task.FromResult(true);
        public Task<bool> SendNewApplicationEmailAsync(string to, string landlordName, string apartmentTitle) => Task.FromResult(true);
        public Task<bool> SendApplicationStatusEmailAsync(string to, string tenantName, string apartmentTitle, string status) => Task.FromResult(true);
        public Task<bool> SendNewMessageEmailAsync(string to, string senderName, string messagePreview) => Task.FromResult(true);
        public Task<bool> SendAppointmentConfirmationEmailAsync(string to, string userName, DateTime appointmentDate, string apartmentTitle) => Task.FromResult(true);
        public Task<bool> SendSavedSearchAlertEmailAsync(string to, int matchCount, string searchCriteria) => Task.FromResult(true);
        public Task<bool> SendListingUnavailableEmailAsync(string to, string userName, string apartmentTitle, string reason) => Task.FromResult(true);
        public Task<bool> SendEmailVerificationAsync(string to, string userName, string verificationLink) => Task.FromResult(true);
        public Task<bool> SendPasswordResetEmailAsync(string to, string userName, string resetLink) => Task.FromResult(true);
        public Task<bool> SendOrderConfirmationEmailAsync(string to, string userName, string planName, string orderNumber, decimal amountEur) => Task.FromResult(true);
    }

    private sealed class NoopReviewFavoriteService : IReviewFavoriteService
    {
        public Task<FavoriteResponse>   CreateFavoriteAsync(CreateFavoriteRequest request)              => Task.FromResult(new FavoriteResponse());
        public Task<ReviewResponse>     CreateReviewAsync(CreateReviewRequest request)                  => Task.FromResult(new ReviewResponse());
        public Task<ReviewResponse>     GetReviewByIdAsync(int reviewId)                                => Task.FromResult(new ReviewResponse());
        public Task<GetReviewsResponse> GetReviewsByApartmentIdAsync(int apartmentId)                   => Task.FromResult(new GetReviewsResponse());
        public Task<DeleteResponse>     DeleteReviewAsync(int reviewId, string callerGuid)              => Task.FromResult(new DeleteResponse());
        public Task<DeleteResponse>     DeleteFavoriteAsync(int favoriteId, string callerGuid)          => Task.FromResult(new DeleteResponse());
        public Task<GetFavoritesResponse> GetUserFavoritesAsync(int userId)                             => Task.FromResult(new GetFavoritesResponse());
    }
}
