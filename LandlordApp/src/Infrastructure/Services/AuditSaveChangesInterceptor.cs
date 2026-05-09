using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Lander.src.Infrastructure.Services;

public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            StampAuditFields(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
            StampAuditFields(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    private void StampAuditFields(DbContext context)
    {
        var callerGuid = GetCallerGuid();
        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                SetIfNullOrDefault(entry, "CreatedDate", now);
                SetIfNullOrDefault(entry, "ModifiedDate", now);
                if (callerGuid.HasValue)
                {
                    SetIfNullOrDefault(entry, "CreatedByGuid", callerGuid);
                    SetIfNullOrDefault(entry, "ModifiedByGuid", callerGuid);
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                // Always stamp modification fields — service layer may have already set them
                // but this ensures nothing slips through (e.g. EF tracking updates)
                SetProperty(entry, "ModifiedDate", now);
                if (callerGuid.HasValue)
                    SetProperty(entry, "ModifiedByGuid", callerGuid);
            }
        }
    }

    private Guid? GetCallerGuid()
    {
        var claim = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub")
            ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var guid) ? guid : null;
    }

    private static void SetIfNullOrDefault(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry,
        string propertyName,
        object? value)
    {
        if (entry.Metadata.FindProperty(propertyName) is null) return;
        var prop = entry.Property(propertyName);
        if (prop.CurrentValue is null || IsDefaultValue(prop.CurrentValue))
            prop.CurrentValue = value;
    }

    private static void SetProperty(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry,
        string propertyName,
        object? value)
    {
        if (entry.Metadata.FindProperty(propertyName) is null) return;
        entry.Property(propertyName).CurrentValue = value;
    }

    private static bool IsDefaultValue(object value) => value switch
    {
        Guid g => g == Guid.Empty,
        DateTime dt => dt == default,
        _ => false
    };
}
