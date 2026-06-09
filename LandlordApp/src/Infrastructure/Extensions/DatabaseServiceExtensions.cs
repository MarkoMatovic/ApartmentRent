using Lander.src.Infrastructure.Services;
using Lander.Helpers;
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

    // SqlServerRetryingExecutionStrategy retries on transient SQL Server errors (network
    // blips, throttling, connection resets).  All manual transactions in service code
    // are now wrapped with UnitOfWorkExtensions.RunInTransactionAsync which calls
    // Database.CreateExecutionStrategy().ExecuteAsync() — the required outer scope for
    // user-initiated transactions when a retry strategy is active.
    private static Action<SqlServerDbContextOptionsBuilder> DbResilience() =>
        sql => sql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
}
