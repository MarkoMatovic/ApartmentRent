using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Communication.Services;

/// <summary>
/// Nightly background service that purges <see cref="Models.EmailLog"/> rows older than
/// the configured retention window.  Prevents unbounded table growth and satisfies GDPR
/// Article 5(1)(e) storage-limitation obligations.
///
/// Configuration (appsettings.json / env):
///   "EmailLog:RetentionDays"   — rows older than this many days are deleted (default 90).
///   "EmailLog:CleanupHourUtc"  — UTC hour at which the nightly run fires (default 3).
/// </summary>
public sealed class EmailLogCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailLogCleanupService> _logger;
    private readonly int _retentionDays;
    private readonly int _cleanupHourUtc;

    public EmailLogCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<EmailLogCleanupService> logger,
        IConfiguration configuration)
    {
        _scopeFactory   = scopeFactory;
        _logger         = logger;
        _retentionDays  = configuration.GetValue("EmailLog:RetentionDays",  90);
        _cleanupHourUtc = configuration.GetValue("EmailLog:CleanupHourUtc",  3);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "EmailLogCleanupService started. RetentionDays={RetentionDays}, CleanupHourUtc={Hour}",
            _retentionDays, _cleanupHourUtc);

        while (!stoppingToken.IsCancellationRequested)
        {
            await WaitUntilNextRunAsync(stoppingToken);

            if (stoppingToken.IsCancellationRequested)
                break;

            await RunCleanupAsync(stoppingToken);
        }
    }

    private async Task WaitUntilNextRunAsync(CancellationToken ct)
    {
        var now  = DateTime.UtcNow;
        var next = now.Date.AddHours(_cleanupHourUtc);
        if (next <= now)
            next = next.AddDays(1);

        var delay = next - now;
        _logger.LogDebug("EmailLogCleanupService: next run in {Delay:hh\\:mm\\:ss} at {Next:u}", delay, next);
        await Task.Delay(delay, ct);
    }

    private async Task RunCleanupAsync(CancellationToken ct)
    {
        try
        {
            var cutoff = DateTime.UtcNow.AddDays(-_retentionDays);
            _logger.LogInformation("EmailLogCleanupService: deleting EmailLog rows with SentAt < {Cutoff:u}", cutoff);

            await using var scope   = _scopeFactory.CreateAsyncScope();
            var             context = scope.ServiceProvider.GetRequiredService<CommunicationsContext>();

            // ExecuteDeleteAsync issues a single DELETE ... WHERE statement — no entity materialisation.
            var deleted = await context.EmailLogs
                .Where(e => e.SentAt < cutoff)
                .ExecuteDeleteAsync(ct);

            _logger.LogInformation("EmailLogCleanupService: deleted {Count} rows (cutoff={Cutoff:u})", deleted, cutoff);
        }
        catch (OperationCanceledException)
        {
            // Shutdown signal — exit gracefully.
        }
        catch (Exception ex)
        {
            // Log but do NOT crash the host — next nightly run will retry.
            _logger.LogError(ex, "EmailLogCleanupService: cleanup run failed");
        }
    }
}
