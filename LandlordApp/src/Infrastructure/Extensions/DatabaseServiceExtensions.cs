using Lander.src.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Lander;

namespace Lander.src.Infrastructure.Extensions;

public static class DatabaseServiceExtensions
{
    public static IServiceCollection AddDatabaseContexts(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<UsersContext>((sp, options) =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), DbResilience())
                   .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

        services.AddDbContext<ApplicationsContext>((sp, options) =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), DbResilience())
                   .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

        services.AddDbContext<ListingsContext>((sp, options) =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), DbResilience())
                   .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

        services.AddDbContext<NotificationContext>((sp, options) =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), DbResilience())
                   .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

        services.AddDbContext<ReviewsContext>((sp, options) =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), DbResilience())
                   .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

        services.AddDbContext<CommunicationsContext>((sp, options) =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), DbResilience())
                   .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

        services.AddDbContext<RoommatesContext>((sp, options) =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), DbResilience())
                   .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

        services.AddDbContext<SearchRequestsContext>((sp, options) =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), DbResilience())
                   .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

        services.AddDbContext<SavedSearchesContext>((sp, options) =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), DbResilience())
                   .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

        services.AddDbContext<AnalyticsContext>((sp, options) =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), DbResilience())
                   .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

        services.AddDbContext<Lander.src.Modules.Appointments.AppointmentsContext>((sp, options) =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), DbResilience())
                   .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

        services.AddDbContext<Lander.src.Modules.Payments.PaymentsContext>((sp, options) =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), DbResilience())
                   .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

        return services;
    }

    // TEMP: retry disabled for k6 performance testing (SqlServerRetryingExecutionStrategy
    // conflicts with manual BeginTransactionAsync used throughout the app)
    private static Action<SqlServerDbContextOptionsBuilder> DbResilience() => _ => { };
}
