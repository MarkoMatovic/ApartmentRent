using System.Text.Json;
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
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessorService> _logger;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);
    private const int MaxRetries = 3;

    public OutboxProcessorService(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessorService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in OutboxProcessorService.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingEventsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var commContext = scope.ServiceProvider.GetRequiredService<CommunicationsContext>();
        var usersContext = scope.ServiceProvider.GetRequiredService<UsersContext>();

        var pending = await commContext.OutboxMessages
            .Where(e => e.ProcessedAt == null && e.RetryCount < MaxRetries)
            .OrderBy(e => e.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        foreach (var evt in pending)
        {
            try
            {
                await HandleEventAsync(evt, usersContext, ct);
                evt.ProcessedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                evt.RetryCount++;
                evt.Error = ex.Message;
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

                // Atomic UPDATE avoids read-modify-write race across concurrent processor instances.
                var rows = await usersContext.Users
                    .Where(u => u.UserId == payload.UserId && u.TokenBalance >= 1)
                    .ExecuteUpdateAsync(s => s.SetProperty(u => u.TokenBalance, u => u.TokenBalance - 1), ct);

                if (rows == 0)
                    throw new InvalidOperationException($"User {payload.UserId} not found or has insufficient tokens.");

                break;

            default:
                throw new InvalidOperationException($"Unknown outbox event type: {evt.EventType}");
        }
    }

    private sealed record SuperLikePayload(int UserId);
}
