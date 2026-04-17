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
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
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
        services.Configure<BrevoSettings>(configuration.GetSection("Brevo"));
        services.AddScoped<IEmailService, EmailService>();

        // --- Apartment notification service ---
        services.AddScoped<IApartmentNotificationService, ApartmentNotificationService>();
        services.AddScoped<IApartmentService, ApartmentService>();

        services.AddHostedService<Lander.src.Infrastructure.Services.DatabaseMigrationService>();
        services.AddHostedService<Lander.src.Modules.Listings.Services.ApartmentCacheWarmupService>();
        services.AddHostedService<Lander.src.Modules.Communication.Services.OutboxProcessorService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IRoommateService, RoommateService>();
        services.AddScoped<ISearchRequestService, SearchRequestService>();
        services.AddScoped<ISavedSearchService, SavedSearchService>();
        services.Configure<TwilioSettings>(configuration.GetSection("Twilio"));
        services.AddScoped<ISmsService, SmsService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<Lander.src.Modules.Analytics.Interfaces.IAnalyticsService, Lander.src.Modules.Analytics.Implementation.AnalyticsService>();
        services.AddScoped<Lander.src.Modules.MachineLearning.Interfaces.IPricePredictionService, Lander.src.Modules.MachineLearning.Implementation.PricePredictionService>();
        services.AddScoped<Lander.src.Modules.MachineLearning.Interfaces.IRoommateMatchingService, Lander.src.Modules.MachineLearning.Implementation.RoommateMatchingService>();

        // .NET 10 Feature: Server-Sent Events for real-time notifications
        services.AddSingleton<NotificationStreamService>();

        services.AddScoped<Lander.src.Modules.ApartmentApplications.Interfaces.IApartmentApplicationService, Lander.src.Modules.ApartmentApplications.Implementation.ApartmentApplicationService>();
        services.AddScoped<Lander.src.Modules.ApartmentApplications.Interfaces.IApplicationApprovalService, Lander.src.Modules.ApartmentApplications.Implementation.ApplicationApprovalService>();

        // --- Payment integration (Monri) ---
        services.AddScoped<Lander.src.Modules.Payments.Interfaces.IMonriPaymentFormService,
                           Lander.src.Modules.Payments.Implementation.MonriPaymentFormService>();
        services.AddScoped<Lander.src.Modules.Payments.Interfaces.IMonriCallbackHandler,
                           Lander.src.Modules.Payments.Implementation.MonriCallbackHandler>();
        services.AddScoped<Lander.src.Modules.Payments.Interfaces.IMonriService,
                           Lander.src.Modules.Payments.Implementation.MonriService>();

        // User deletion handlers (decoupled cleanup via IUserDeletedHandler)
        services.AddScoped<Lander.src.Common.IUserDeletedHandler, Lander.src.Modules.Listings.Implementation.ApartmentUserDeletedHandler>();
        services.AddScoped<Lander.src.Common.IUserDeletedHandler, Lander.src.Modules.Roommates.Implementation.RoommateUserDeletedHandler>();

        // .NET 10 Feature: Vector Search for semantic apartment search
        services.AddSingleton<Lander.src.Modules.MachineLearning.Services.SimpleEmbeddingService>();

        // Appointment Booking System
        services.AddScoped<Lander.src.Modules.Appointments.Interfaces.IAppointmentService, Lander.src.Modules.Appointments.Implementation.AppointmentService>();

        // Payments
        services.AddScoped<Lander.src.Modules.Payments.Interfaces.IPaymentService, Lander.src.Modules.Payments.Implementation.PaytenPaymentService>();

        services.AddScoped<TokenProvider>();
        services.AddScoped<RefreshTokenService>();
        services.AddHttpContextAccessor();

        // RBAC: Authorization Infrastructure
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, ApartmentOwnerHandler>();

        // Testable time abstraction — use FakeTimeProvider from Microsoft.Extensions.TimeProvider.Testing in tests
        services.AddSingleton(TimeProvider.System);

        // HybridCache: stampede-safe L1 cache with Redis-ready L2 support
        services.AddHybridCache();

        return services;
    }
}
