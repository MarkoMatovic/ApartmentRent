using Microsoft.EntityFrameworkCore;

namespace Lander.src.Infrastructure.Services;

/// <summary>
/// IHostedService koji automatski pokреće sve EF Core migracije pri startu aplikacije.
/// Sve greške se loguju ali ne zaustavljaju pokretanje app-a — operator mora ručno da ispravi.
/// </summary>
public class DatabaseMigrationService(
    IServiceScopeFactory scopeFactory,
    ILogger<DatabaseMigrationService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("DatabaseMigrationService: pokrećem migracije za sve DbContext-e...");

        using var scope = scopeFactory.CreateScope();

        await MigrateAsync<UsersContext>(scope, cancellationToken);
        await MigrateAsync<ApplicationsContext>(scope, cancellationToken);
        await MigrateAsync<ListingsContext>(scope, cancellationToken);
        await MigrateAsync<NotificationContext>(scope, cancellationToken);
        await MigrateAsync<ReviewsContext>(scope, cancellationToken);
        await MigrateAsync<CommunicationsContext>(scope, cancellationToken);
        await MigrateAsync<RoommatesContext>(scope, cancellationToken);
        await MigrateAsync<SearchRequestsContext>(scope, cancellationToken);
        await MigrateAsync<SavedSearchesContext>(scope, cancellationToken);
        await MigrateAsync<AnalyticsContext>(scope, cancellationToken);
        await MigrateAsync<Lander.src.Modules.Appointments.AppointmentsContext>(scope, cancellationToken);
        await MigrateAsync<Lander.src.Modules.Payments.PaymentsContext>(scope, cancellationToken);

        logger.LogInformation("DatabaseMigrationService: sve migracije završene.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task MigrateAsync<TContext>(IServiceScope scope, CancellationToken ct)
        where TContext : DbContext
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<TContext>();
            var pending = (await db.Database.GetPendingMigrationsAsync(ct)).ToList();

            if (pending.Count > 0)
            {
                logger.LogInformation(
                    "Migriram {Context}: {Count} pending migracija ({Migrations})",
                    typeof(TContext).Name, pending.Count, string.Join(", ", pending));

                await db.Database.MigrateAsync(ct);

                logger.LogInformation("{Context}: migracija završena uspešno.", typeof(TContext).Name);
            }
            else
            {
                logger.LogDebug("{Context}: nema pending migracija.", typeof(TContext).Name);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Greška pri migraciji {Context}. Aplikacija nastavlja s radom, ali DB možda nije ažuran.",
                typeof(TContext).Name);
        }
    }
}
