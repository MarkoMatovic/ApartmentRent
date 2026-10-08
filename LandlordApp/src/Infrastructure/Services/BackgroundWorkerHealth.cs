using System.Collections.Concurrent;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Lander.src.Infrastructure.Services;

/// <summary>
/// Tracks consecutive failures of long-running background loops.
///
/// Why this exists: <c>HostOptions.BackgroundServiceExceptionBehavior = Ignore</c> (needed so a
/// shutdown-time cancellation does not crash the host) also means a worker that fails on every
/// poll never surfaces anywhere — the outbox processor failed with a SQL error on each 10 s cycle
/// for months and nothing noticed. Workers report here; <see cref="BackgroundWorkersHealthCheck"/>
/// turns repeated failures into a visible <c>/health</c> state, and workers log at Critical once the
/// threshold is crossed so a log alert can fire.
/// </summary>
public sealed class BackgroundWorkerHealth
{
    /// <summary>Consecutive failed runs after which a worker is reported as degraded.</summary>
    public const int FailureThreshold = 3;

    private sealed class State
    {
        public readonly object Lock = new();
        public int ConsecutiveFailures;
        public DateTime? LastFailureUtc;
        public DateTime? LastSuccessUtc;
        public string? LastError;
    }

    private readonly ConcurrentDictionary<string, State> _workers = new();
    private readonly TimeProvider _time;

    public BackgroundWorkerHealth(TimeProvider time) => _time = time;

    public void RecordSuccess(string worker)
    {
        var s = _workers.GetOrAdd(worker, _ => new State());
        lock (s.Lock)
        {
            s.ConsecutiveFailures = 0;
            s.LastSuccessUtc = _time.GetUtcNow().UtcDateTime;
        }
    }

    /// <returns>The number of consecutive failures including this one.</returns>
    public int RecordFailure(string worker, Exception ex)
    {
        var s = _workers.GetOrAdd(worker, _ => new State());
        lock (s.Lock)
        {
            s.ConsecutiveFailures++;
            s.LastFailureUtc = _time.GetUtcNow().UtcDateTime;
            s.LastError = ex.Message.Length > 300 ? ex.Message[..300] : ex.Message;
            return s.ConsecutiveFailures;
        }
    }

    public IReadOnlyList<WorkerStatus> Snapshot() =>
        _workers.Select(kv =>
        {
            lock (kv.Value.Lock)
                return new WorkerStatus(kv.Key, kv.Value.ConsecutiveFailures,
                    kv.Value.LastFailureUtc, kv.Value.LastSuccessUtc, kv.Value.LastError);
        }).ToList();
}

public sealed record WorkerStatus(
    string Name, int ConsecutiveFailures, DateTime? LastFailureUtc, DateTime? LastSuccessUtc, string? LastError);

/// <summary>
/// Reports <c>Degraded</c> while any background worker has failed
/// <see cref="BackgroundWorkerHealth.FailureThreshold"/> or more runs in a row.
/// Degraded (not Unhealthy) on purpose: a stuck outbox must be visible, but restarting the
/// whole web app because of it would only hurt users.
/// </summary>
public sealed class BackgroundWorkersHealthCheck : IHealthCheck
{
    private readonly BackgroundWorkerHealth _health;

    public BackgroundWorkersHealthCheck(BackgroundWorkerHealth health) => _health = health;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var failing = _health.Snapshot()
            .Where(w => w.ConsecutiveFailures >= BackgroundWorkerHealth.FailureThreshold)
            .ToList();

        if (failing.Count == 0)
            return Task.FromResult(HealthCheckResult.Healthy("All background workers are running."));

        var data = failing.ToDictionary(
            w => w.Name,
            w => (object)$"{w.ConsecutiveFailures} consecutive failures; last error: {w.LastError}");

        return Task.FromResult(HealthCheckResult.Degraded(
            $"{failing.Count} background worker(s) failing: {string.Join(", ", failing.Select(w => w.Name))}",
            data: data));
    }
}
