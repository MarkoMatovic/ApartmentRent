using Hangfire;
using Lander.Helpers;
using Lander.src.Infrastructure.Authorization;
using Lander.src.Infrastructure.Services;
using Microsoft.Extensions.Caching.Hybrid;
using Lander.src.Modules.Communication.Implementation;
using Lander.src.Modules.Communication.Interfaces;
using Lander.src.Modules.Communication.Services;
using Lander.src.Modules.Listings.Implementation;
using Lander.src.Modules.Listings.Interfaces;
using Lander.src.Modules.Listings.Services;
using Lander.src.Modules.Reviews.Client;
using Lander.src.Modules.Reviews.Implementation;
using Lander.src.Modules.Roommates.Implementation;
using Lander.src.Modules.Roommates.Interfaces;
using Lander.src.Modules.SavedSearches.Implementation;
using Lander.src.Modules.SavedSearches.Interfaces;
using Lander.src.Modules.SearchRequests.Implementation;
using Lander.src.Modules.SearchRequests.Interfaces;
using Lander.src.Modules.Users.Domain.IRepository;
using Lander.src.Modules.Users.Domain.IService;
using Lander.src.Modules.Users.Implementation.PermissionImplementation;
using Lander.src.Modules.Users.Implementation.UserImplementation;
using Lander.src.Modules.Users.Infrastructure.Repository;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Lander.src.Modules.Users.Services;
using Lander.src.Notifications.Implementation;
using Lander.src.Notifications.Interfaces;
using Lander.src.Notifications.Services;
using Microsoft.AspNetCore.Authorization;

namespace Lander.src.Infrastructure.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
    {
        // ── Options validation (fail fast on startup with a clear message) ────────────
        // In dev/E2eTesting we use DevEmailService and no blob storage, so we only
        // enforce required-field validation in production to avoid blocking local startup.
        var isProd = !env.IsDevelopment() && !env.IsEnvironment("E2eTesting");

        var brevo = services.AddOptions<BrevoSettings>()
            .BindConfiguration("Brevo")
            .ValidateDataAnnotations();
        if (isProd) brevo.ValidateOnStart();

        var blob = services.AddOptions<Lander.src.Infrastructure.FileStorage.AzureBlobStorageOptions>()
            .BindConfiguration("AzureBlobStorage")
            .ValidateDataAnnotations();
        if (isProd) blob.ValidateOnStart();

        // TwilioSettings: optional feature — no ValidateOnStart, bind only.
        services.AddOptions<TwilioSettings>()
            .BindConfiguration("Twilio");

        // --- Password hashing ---
        services.AddScoped<IPasswordHashingService, PasswordHashingService>();

        // --- User sub-services (registered before UserService facade) ---
        services.AddScoped<IUserRoleUpgradeService, UserRoleUpgradeService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserProfileService, UserProfileService>();

        // --- UserService facade (depends on the three sub-services above) ---
        services.AddScoped<IUserInterface, UserService>();

        // --- RBAC ---
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IPermissionService, PermissionService>();

        // --- Email template renderer + email service ---
        services.AddScoped<IEmailTemplateRenderer, EmailTemplateRenderer>();
        // #7: DevEmailService in dev/test — logs + writes HTML file; never sends real mail.
        if (env.IsDevelopment() || env.IsEnvironment("E2eTesting"))
            services.AddScoped<IEmailService, DevEmailService>();
        else
            services.AddScoped<IEmailService, EmailService>();

        // --- Apartment notification service ---
        services.AddScoped<IApartmentNotificationService, ApartmentNotificationService>();

        // Cross-module providers for Listings — isolate bounded context from raw DbContexts.
        services.AddScoped<IReviewStatsProvider, ReviewStatsProvider>();
        services.AddScoped<IListingsUserLookup, ListingsUserLookup>();

        services.AddSingleton<Lander.src.Modules.Listings.Services.ApartmentCacheVersionService>();
        services.AddScoped<Lander.src.Infrastructure.Services.IAuditLogService, Lander.src.Infrastructure.Services.AuditLogService>();
        // Register ApartmentService under all three interfaces so consumers can inject
        // the narrower IApartmentQueryService / IApartmentCommandService directly.
        services.AddScoped<ApartmentService>();
        services.AddScoped<IApartmentService>(sp => sp.GetRequiredService<ApartmentService>());
        services.AddScoped<IApartmentQueryService>(sp => sp.GetRequiredService<ApartmentService>());
        services.AddScoped<IApartmentCommandService>(sp => sp.GetRequiredService<ApartmentService>());

        services.AddHostedService<Lander.src.Infrastructure.Services.DatabaseMigrationService>();
        services.AddHostedService<Lander.src.Modules.Listings.Services.ApartmentCacheWarmupService>();
        services.AddHostedService<Lander.src.Modules.Communication.Services.OutboxProcessorService>();
        // Nightly jobs migrated to Hangfire recurring jobs (registered in Program.cs).
        services.AddTransient<Lander.src.Modules.MachineLearning.Services.PriceModelTrainingService>();
        services.AddTransient<Lander.src.Modules.Communication.Services.EmailLogCleanupService>();
        services.AddTransient<Lander.src.Modules.Listings.Services.ListingExpirationService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IRoommateService, RoommateService>();
        services.AddScoped<ISearchRequestService, SearchRequestService>();
        services.AddScoped<ISavedSearchService, SavedSearchService>();
        services.AddScoped<ISmsService, SmsService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<Lander.src.Modules.Analytics.Interfaces.IAnalyticsService, Lander.src.Modules.Analytics.Implementation.AnalyticsService>();
        services.AddScoped<Lander.src.Modules.MachineLearning.Interfaces.IPricePredictionService, Lander.src.Modules.MachineLearning.Implementation.PricePredictionService>();
        services.AddScoped<Lander.src.Modules.MachineLearning.Interfaces.IRoommateMatchingService, Lander.src.Modules.MachineLearning.Implementation.RoommateMatchingService>();

        // --- Neighbourhood Insights (Walk Score + OpenStreetMap Overpass) ---
        services.AddScoped<INeighbourhoodService, Lander.src.Modules.Listings.Services.NeighbourhoodService>();
        services.AddHttpClient("WalkScore", c =>
        {
            c.Timeout = TimeSpan.FromSeconds(5);
            c.DefaultRequestHeaders.Add("Accept", "application/json");
        });
        services.AddHttpClient("Overpass", c =>
        {
            c.BaseAddress = new Uri("https://overpass-api.de/");
            c.Timeout     = TimeSpan.FromSeconds(15);
            c.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        // .NET 10 Feature: Server-Sent Events for real-time notifications
        services.AddSingleton<NotificationStreamService>();

        services.AddScoped<Lander.src.Modules.ApartmentApplications.Interfaces.IApartmentApplicationService, Lander.src.Modules.ApartmentApplications.Implementation.ApartmentApplicationService>();
        services.AddScoped<Lander.src.Modules.ApartmentApplications.Interfaces.IApplicationApprovalService, Lander.src.Modules.ApartmentApplications.Implementation.ApplicationApprovalService>();

        // --- File storage (Azure Blob in prod, local disk fallback in dev) ---
        var blobConnectionString = configuration["AzureBlobStorage:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(blobConnectionString))
            services.AddSingleton<Lander.src.Infrastructure.FileStorage.IFileStorageService,
                                  Lander.src.Infrastructure.FileStorage.AzureBlobStorageService>();
        else
            services.AddSingleton<Lander.src.Infrastructure.FileStorage.IFileStorageService,
                                  Lander.src.Infrastructure.FileStorage.LocalFileStorageService>();
        services.AddScoped<Lander.src.Infrastructure.FileStorage.IImageUrlBuilder,
                           Lander.src.Infrastructure.FileStorage.ImageUrlBuilder>();
        services.AddScoped<Lander.src.Modules.Listings.Services.ApartmentImageBlobMigrationService>();

        // --- Payments (provider-agnostic) ---
        // The Monri provider was removed. PaymentService exposes plans/status/orders/cancel;
        // PaymentFulfillmentService grants purchases. A future payment provider plugs in by
        // confirming charges and calling IPaymentFulfillmentService.FulfillAsync(...).
        services.AddScoped<Lander.src.Modules.Payments.Interfaces.IPaymentService,
                           Lander.src.Modules.Payments.Implementation.PaymentService>();
        services.AddScoped<Lander.src.Modules.Payments.Interfaces.IPaymentFulfillmentService,
                           Lander.src.Modules.Payments.Implementation.PaymentFulfillmentService>();
        services.AddTransient<Lander.src.Modules.Payments.Services.PremiumExpirationService>();

        // User deletion handlers (decoupled cleanup via IUserDeletedHandler)
        services.AddScoped<Lander.src.Common.IUserDeletedHandler, Lander.src.Modules.Listings.Implementation.ApartmentUserDeletedHandler>();
        services.AddScoped<Lander.src.Common.IUserDeletedHandler, Lander.src.Modules.Roommates.Implementation.RoommateUserDeletedHandler>();
        services.AddScoped<Lander.src.Common.IUserDeletedHandler, Lander.src.Modules.Communication.Implementation.MessageUserDeletedHandler>();
        services.AddScoped<Lander.src.Common.IUserDeletedHandler, Lander.src.Notifications.NotificationUserDeletedHandler>();
        services.AddScoped<Lander.src.Common.IUserDeletedHandler, Lander.src.Modules.Analytics.AnalyticsUserDeletedHandler>();
        services.AddScoped<Lander.src.Common.IUserDeletedHandler, Lander.src.Modules.ApartmentApplications.ApplicationUserDeletedHandler>();

        // gRPC client for Reviews/Favorites microservice.
        // Registered as SINGLETON so that GrpcChannel (which owns the HTTP/2 connection
        // pool) is created once and reused — creating a new channel per-request leaks
        // sockets.  IHttpContextAccessor is injected so each call can forward the
        // caller's Authorization header at invoke time.
        services.AddSingleton<IGrpcServiceClient>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var accessor = sp.GetRequiredService<IHttpContextAccessor>();
            var grpcUrl = config["GrpcServerUrl"] ?? "http://localhost:5001";
            return new GrpcServiceClient(grpcUrl, accessor);
        });

        // .NET 10 Feature: Vector Search for semantic apartment search
        services.AddSingleton<Lander.src.Modules.MachineLearning.Services.SimpleEmbeddingService>();

        // Appointment Booking System
        services.AddScoped<Lander.src.Modules.Appointments.Interfaces.IAppointmentService, Lander.src.Modules.Appointments.Implementation.AppointmentService>();

        services.AddScoped<TokenProvider>();
        services.AddScoped<RefreshTokenService>();
        services.AddSingleton<IJwtBlacklistService, JwtBlacklistService>();
        services.AddHttpContextAccessor();
        services.AddSingleton<Lander.src.Infrastructure.Services.AuditSaveChangesInterceptor>();

        // RBAC: Authorization Infrastructure
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, ApartmentOwnerHandler>();

        // Testable time abstraction — use FakeTimeProvider from Microsoft.Extensions.TimeProvider.Testing in tests
        services.AddSingleton(TimeProvider.System);

        // HybridCache: stampede-safe L1 cache with Redis-ready L2 support
        services.AddHybridCache();

        // Fire-and-forget background jobs backed by Hangfire
        services.AddScoped<IBackgroundScheduler, HangfireBackgroundScheduler>();

        return services;
    }
}
