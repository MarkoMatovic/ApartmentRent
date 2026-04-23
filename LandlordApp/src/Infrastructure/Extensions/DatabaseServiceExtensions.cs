using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Lander;

namespace Lander.src.Infrastructure.Extensions;

public static class DatabaseServiceExtensions
{
    public static IServiceCollection AddDatabaseContexts(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<UsersContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), DbResilience()));

        services.AddDbContext<ApplicationsContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), DbResilience()));

        services.AddDbContext<ListingsContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), DbResilience()));

        services.AddDbContext<NotificationContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), DbResilience()));

        services.AddDbContext<ReviewsContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), DbResilience()));

        services.AddDbContext<CommunicationsContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), DbResilience()));

        services.AddDbContext<RoommatesContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), DbResilience()));

        services.AddDbContext<SearchRequestsContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), DbResilience()));

        services.AddDbContext<SavedSearchesContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), DbResilience()));

        services.AddDbContext<AnalyticsContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), DbResilience()));

        services.AddDbContext<Lander.src.Modules.Appointments.AppointmentsContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), DbResilience()));

        services.AddDbContext<Lander.src.Modules.Payments.PaymentsContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), DbResilience()));

        return services;
    }

    // TEMP: retry disabled for k6 performance testing (SqlServerRetryingExecutionStrategy
    // conflicts with manual BeginTransactionAsync used throughout the app)
    private static Action<SqlServerDbContextOptionsBuilder> DbResilience() => _ => { };
}
