namespace Lander.src.Infrastructure.Services;

public interface IAuditLogService
{
    void Log(string action, string entityType, object entityId, string? performedByGuid = null, string? details = null);
}

/// <summary>
/// Writes structured audit events to a dedicated logger category so they can be
/// routed to a separate sink (e.g. a dedicated DB table or log stream) via Serilog config.
/// </summary>
public sealed class AuditLogService : IAuditLogService
{
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(ILogger<AuditLogService> logger)
    {
        _logger = logger;
    }

    public void Log(string action, string entityType, object entityId, string? performedByGuid = null, string? details = null)
    {
        _logger.LogInformation(
            "[AUDIT] Action={Action} EntityType={EntityType} EntityId={EntityId} PerformedBy={PerformedBy} Details={Details}",
            action, entityType, entityId, performedByGuid ?? "system", details ?? string.Empty);
    }
}
