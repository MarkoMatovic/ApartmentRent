using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Communication.Services;

/// <summary>
/// Hangfire recurring job (daily 03:00 UTC) that purges <see cref="Models.EmailLog"/> rows older
/// than the configured retention window. Prevents unbounded table growth and satisfies GDPR
/// Article 5(1)(e) storage-limitation obligations.
///
/// Configuration: "EmailLog:RetentionDays" (default 90).
/// </summary>
public sealed class EmailLogCleanupService
{
    private readonly CommunicationsContext _context;
    private readonly ILogger<EmailLogCleanupService> _logger;
    private readonly IConfiguration _configuration;

    public EmailLogCleanupService(
        CommunicationsContext context,
        ILogger<EmailLogCleanupService> logger,
        IConfiguration configuration)
    {
        _context       = context;
        _logger        = logger;
        _configuration = configuration;
    }

    public async Task RunAsync()
    {
        var retentionDays = _configuration.GetValue("EmailLog:RetentionDays", 90);
        var cutoff        = DateTime.UtcNow.AddDays(-retentionDays);

        _logger.LogInformation("EmailLogCleanup: deleting rows with SentAt < {Cutoff:u}", cutoff);

        var deleted = await _context.EmailLogs
            .Where(e => e.SentAt < cutoff)
            .ExecuteDeleteAsync();

        _logger.LogInformation("EmailLogCleanup: deleted {Count} rows.", deleted);
    }
}
