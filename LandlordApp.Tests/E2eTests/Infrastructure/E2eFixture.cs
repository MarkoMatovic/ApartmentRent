using DotNet.Testcontainers.Builders;
using Lander;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Respawn;
using Testcontainers.MsSql;

namespace LandlordApp.Tests.E2eTests.Infrastructure;

/// <summary>
/// Collection-scoped fixture: starts a SQL Server container once, applies DB schema via EnsureCreated,
/// seeds lookup data, and exposes Respawn for per-test state reset.
///
/// Lifecycle:
///   InitializeAsync  → container start → factory boot → schema create → seed roles → respawn init
///   DisposeAsync     → factory dispose → container stop
/// </summary>
public class E2eFixture : IAsyncLifetime
{
    // ── Container ────────────────────────────────────────────────────────────

    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithCleanUp(true)
        .Build();

    // ── Public surface ───────────────────────────────────────────────────────

    public E2eWebApplicationFactory Factory { get; private set; } = null!;
    public string ConnectionString { get; private set; } = string.Empty;

    private Respawner _respawner = null!;

    // ── IAsyncLifetime ────────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();
        ConnectionString = _sqlContainer.GetConnectionString();

        Factory = new E2eWebApplicationFactory(ConnectionString);

        // Trigger the WebApplicationFactory host build (ensures DI is wired)
        _ = Factory.Server;

        // Create schema for all 12 DbContexts (EnsureCreated = no migration files needed)
        using var scope = Factory.Services.CreateScope();
        await CreateAllSchemasAsync(scope.ServiceProvider);

        // Seed invariant lookup data that Respawn must not erase
        await SeedBaseDataAsync(scope.ServiceProvider);

        // Initialise Respawn AFTER schema exists
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();
        _respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter     = DbAdapter.SqlServer,
            // Keep migration history and role seed data intact
            TablesToIgnore =
            [
                new Respawn.Graph.Table("__EFMigrationsHistory"),
                new Respawn.Graph.Table("Roles"),
            ],
        });
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _sqlContainer.DisposeAsync();
    }

    // ── Per-test reset ────────────────────────────────────────────────────────

    /// <summary>
    /// Called by <see cref="E2eTestBase"/> before every test.
    /// Clears all data tables and resets every mock back to a pristine state.
    /// </summary>
    public async Task ResetStateAsync()
    {
        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();
        await _respawner.ResetAsync(conn);

        Factory.ResetMocks();
    }

    // ── Schema helpers ────────────────────────────────────────────────────────

    private static async Task CreateAllSchemasAsync(IServiceProvider sp)
    {
        // EnsureCreated has a known limitation with multiple DbContexts sharing one database:
        // it calls HasTables() internally — if the DB already has ANY table (created by the
        // first context), subsequent calls are a complete no-op. Only the first context's
        // tables ever get created.
        //
        // Fix: use EnsureCreated only for the first context (creates the DB + its schema),
        // then call CreateTablesAsync() directly for all remaining contexts.
        // CreateTablesAsync() skips the HasTables() guard and creates only the tables
        // that belong to the given context (navigation properties marked [NotMapped] are
        // excluded, so there is no risk of duplicate-table errors across contexts).

        var usersCtx = sp.GetRequiredService<UsersContext>();
        await usersCtx.Database.EnsureCreatedAsync(); // creates the SQL Server database itself

        // All remaining contexts — create their own tables directly
        var remaining = new DbContext[]
        {
            sp.GetRequiredService<ListingsContext>(),
            sp.GetRequiredService<CommunicationsContext>(),
            sp.GetRequiredService<ApplicationsContext>(),
            sp.GetRequiredService<NotificationContext>(),
            sp.GetRequiredService<ReviewsContext>(),
            sp.GetRequiredService<RoommatesContext>(),
            sp.GetRequiredService<SearchRequestsContext>(),
            sp.GetRequiredService<SavedSearchesContext>(),
            sp.GetRequiredService<AnalyticsContext>(),
            sp.GetRequiredService<Lander.src.Modules.Appointments.AppointmentsContext>(),
            sp.GetRequiredService<Lander.src.Modules.Payments.PaymentsContext>(),
        };

        foreach (var ctx in remaining)
        {
            var creator = ctx.Database.GetService<IRelationalDatabaseCreator>();
            await creator.CreateTablesAsync();
        }
    }

    private static async Task SeedBaseDataAsync(IServiceProvider sp)
    {
        var ctx = sp.GetRequiredService<UsersContext>();

        if (!await ctx.Roles.AnyAsync())
        {
            ctx.Roles.AddRange(
                new Role { RoleName = "Tenant",   Description = "Tenant role" },
                new Role { RoleName = "Landlord",  Description = "Landlord role" },
                new Role { RoleName = "Admin",     Description = "Admin role" }
            );
            await ctx.SaveChangesAsync();
        }
    }
}
