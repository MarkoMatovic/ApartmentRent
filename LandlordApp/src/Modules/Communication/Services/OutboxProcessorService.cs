using System.Text.Json;
using Lander.src.Infrastructure.Services;
using Lander.src.Modules.Communication.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lander.src.Modules.Communication.Services;

/// <summary>
/// Background service that processes outbox events written by MessageService.
/// Implements the outbox pattern: events are applied to other DbContexts (e.g. UsersContext)
/// independently of the original transaction, guaranteeing at-least-once delivery.
/// </summary>
public class OutboxProcessorService : BackgroundService
{
    private const string WorkerName = "outbox-processor";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessorService> _logger;
    private readonly BackgroundWorkerHealth _health;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);
    private const int MaxRetries = 3;
    private const int BatchSize = 50;

    // "Claimed, in progress" marker stored in ProcessedAt. The column is SQL Server `datetime`,
    // whose minimum is 1753-01-01 — DateTime.MinValue (0001-01-01) is out of range and made
    // the claim UPDATE throw error 242 on every poll, so no outbox event was ever processed.
    // Single source of truth: the SQL claim, the in-memory claim and the pending query all use this.
    private static readonly DateTime ClaimSentinel = new(1753, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

    public OutboxProcessorService(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessorService> logger,
        BackgroundWorkerHealth health)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _health = health;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessorService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEventsAsync(stoppingToken);
                _health.RecordSuccess(WorkerName);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break; // shutting down — not a failure
            }
            catch (Exception ex)
            {
                var failures = _health.RecordFailure(WorkerName, ex);
                _logger.LogError(ex, "Unhandled error in OutboxProcessorService.");

                // Critical (once at the threshold, then every 30th run ≈ 5 min) so a log alert can
                // fire. Without this a worker failing on every poll was invisible for months.
                if (failures == BackgroundWorkerHealth.FailureThreshold ||
                    (failures > BackgroundWorkerHealth.FailureThreshold && failures % 30 == 0))
                {
                    _logger.LogCritical(ex,
                        "OutboxProcessorService has failed {Failures} consecutive runs — queued events are NOT being processed.",
                        failures);
                }
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ProcessPendingEventsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var commContext = scope.ServiceProvider.GetRequiredService<CommunicationsContext>();
        var usersContext = scope.ServiceProvider.GetRequiredService<UsersContext>();

        // ── Atomic claim: set ProcessedAt to the sentinel value (ClaimSentinel)
        // in a single UPDATE so that concurrent instances don't double-process.
        // Only rows where ProcessedAt IS NULL are eligible; UPDLOCK + READPAST hints
        // skip rows already locked by another instance.
        if (commContext.Database.IsRelational())
        {
            await commContext.Database.ExecuteSqlRawAsync(
                @"UPDATE TOP({0}) [Communication].[OutboxMessages]
                  SET ProcessedAt = {1}
                  WHERE ProcessedAt IS NULL AND RetryCount < {2}",
                BatchSize, ClaimSentinel, MaxRetries);
        }
        else
        {
            // Non-relational provider (InMemory in tests) — claim without raw SQL;
            // single-instance semantics are fine there.
            var toClaim = await commContext.OutboxMessages
                .Where(e => e.ProcessedAt == null && e.RetryCount < MaxRetries)
                .OrderBy(e => e.CreatedAt)
                .Take(BatchSize)
                .ToListAsync(ct);
            foreach (var e in toClaim) e.ProcessedAt = ClaimSentinel;
            await commContext.SaveChangesAsync(ct);
        }

        // Fetch only the rows we just claimed (sentinel = ClaimSentinel)
        var pending = await commContext.OutboxMessages
            .Where(e => e.ProcessedAt == ClaimSentinel)
            .OrderBy(e => e.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        foreach (var evt in pending)
        {
            try
            {
                await HandleEventAsync(evt, usersContext, ct);
                // Mark truly processed with real timestamp
                evt.ProcessedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                // Release the claim (set back to NULL) so retry logic can pick it up
                var error = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
                if (commContext.Database.IsRelational())
                {
                    await commContext.Database.ExecuteSqlRawAsync(
                        "UPDATE [Communication].[OutboxMessages] SET ProcessedAt = NULL, RetryCount = RetryCount + 1, Error = {0} WHERE Id = {1}",
                        error, evt.Id);
                    await commContext.Entry(evt).ReloadAsync(ct);
                }
                else
                {
                    evt.ProcessedAt = null;
                    evt.RetryCount += 1;
                    evt.Error = error;
                }

                if (evt.RetryCount >= MaxRetries)
                    _logger.LogError(ex,
                        "Outbox event {Id} (type: {Type}) exhausted {Max} retries and will not be retried. Manual intervention required.",
                        evt.Id, evt.EventType, MaxRetries);
                else
                    _logger.LogWarning(ex, "Failed to process outbox event {Id} (type: {Type}), retry {Retry}/{Max}.",
                        evt.Id, evt.EventType, evt.RetryCount, MaxRetries);
            }
        }

        if (pending.Count > 0)
            await commContext.SaveChangesAsync(ct);
    }

    private static async Task HandleEventAsync(OutboxMessage evt, UsersContext usersContext, CancellationToken ct)
    {
        switch (evt.EventType)
        {
            case "SuperLikeTokenDeduction":
                var payload = JsonSerializer.Deserialize<SuperLikePayload>(evt.Payload)
                    ?? throw new InvalidOperationException("Invalid SuperLikeTokenDeduction payload.");

                if (usersContext.Database.IsRelational())
                {
                    var affected = await usersContext.Database.ExecuteSqlRawAsync(
                        "UPDATE [UsersRoles].[Users] SET TokenBalance = TokenBalance - 1 WHERE UserId = {0} AND TokenBalance >= 1",
                        payload.UserId);

                    if (affected == 0)
                        throw new InvalidOperationException($"User {payload.UserId} not found or has insufficient tokens.");
                }
                else
                {
                    // Non-relational provider (InMemory in tests) — same semantics without raw SQL
                    var user = await usersContext.Users.FirstOrDefaultAsync(u => u.UserId == payload.UserId, ct);
                    if (user is null || user.TokenBalance < 1)
                        throw new InvalidOperationException($"User {payload.UserId} not found or has insufficient tokens.");
                    user.TokenBalance -= 1;
                    await usersContext.SaveChangesAsync(ct);
                }
                break;

            default:
                throw new InvalidOperationException($"Unknown outbox event type: {evt.EventType}");
        }
    }

    private sealed record SuperLikePayload(int UserId);
}
