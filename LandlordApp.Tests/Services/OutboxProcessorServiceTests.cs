using FluentAssertions;
using Lander;
using Lander.src.Modules.Communication.Models;
using Lander.src.Modules.Communication.Services;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace LandlordApp.Tests.Services;

public class OutboxProcessorServiceTests
{
    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a real IServiceScopeFactory backed by in-memory EF databases.
    /// </summary>
    private static (IServiceScopeFactory scopeFactory, CommunicationsContext commsCtx, UsersContext usersCtx)
        BuildScopeFactory()
    {
        var commsOpts = new DbContextOptionsBuilder<CommunicationsContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var usersOpts = new DbContextOptionsBuilder<UsersContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var commsCtx = new CommunicationsContext(commsOpts);
        var usersCtx = new UsersContext(usersOpts);

        var services = new ServiceCollection();
        services.AddSingleton(commsCtx);
        services.AddSingleton(usersCtx);

        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        return (scopeFactory, commsCtx, usersCtx);
    }

    private static OutboxMessage SuperLikeEvent(int userId) => new()
    {
        EventType = "SuperLikeTokenDeduction",
        Payload   = JsonSerializer.Serialize(new { UserId = userId }),
        CreatedAt = DateTime.UtcNow
    };

    private static User MakeUser(int userId, int tokens = 5) => new()
    {
        UserId       = userId,
        UserGuid     = Guid.NewGuid(),
        FirstName    = "Test",
        LastName     = "User",
        Email        = $"user{userId}@test.com",
        Password     = "hash",
        IsActive     = true,
        TokenBalance = tokens,
        CreatedDate  = DateTime.UtcNow
    };

    private static OutboxProcessorService CreateService(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessorService>? logger = null)
    {
        logger ??= new Mock<ILogger<OutboxProcessorService>>().Object;
        return new OutboxProcessorService(scopeFactory, logger);
    }

    // ─── ProcessPendingEventsAsync — happy path ───────────────────────────────

    [Fact]
    public async Task ProcessPendingEvents_SuperLikeTokenDeduction_DecrementsUserBalance()
    {
        var (scopeFactory, commsCtx, usersCtx) = BuildScopeFactory();

        // Seed user with 3 tokens
        usersCtx.Users.Add(MakeUser(userId: 1, tokens: 3));
        await usersCtx.SaveChangesAsync();

        // Seed one pending outbox event
        commsCtx.OutboxMessages.Add(SuperLikeEvent(1));
        await commsCtx.SaveChangesAsync();

        var svc = CreateService(scopeFactory);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Run one poll cycle — cancel before the 10-second wait triggers again
        var executeTask = svc.StartAsync(cts.Token);
        await Task.Delay(200, CancellationToken.None);
        cts.Cancel();
        await svc.StopAsync(CancellationToken.None);

        usersCtx.ChangeTracker.Clear();
        var user = await usersCtx.Users.FindAsync(1);
        user!.TokenBalance.Should().Be(2);

        var evt = await commsCtx.OutboxMessages.FirstAsync();
        evt.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessPendingEvents_EventMarkedAsProcessed_NotReprocessed()
    {
        var (scopeFactory, commsCtx, usersCtx) = BuildScopeFactory();

        usersCtx.Users.Add(MakeUser(userId: 2, tokens: 5));
        await usersCtx.SaveChangesAsync();

        // Already processed event
        commsCtx.OutboxMessages.Add(new OutboxMessage
        {
            EventType   = "SuperLikeTokenDeduction",
            Payload     = JsonSerializer.Serialize(new { UserId = 2 }),
            CreatedAt   = DateTime.UtcNow.AddMinutes(-1),
            ProcessedAt = DateTime.UtcNow // already done
        });
        await commsCtx.SaveChangesAsync();

        var svc = CreateService(scopeFactory);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var executeTask = svc.StartAsync(cts.Token);
        await Task.Delay(200, CancellationToken.None);
        cts.Cancel();
        await svc.StopAsync(CancellationToken.None);

        // Token should stay at 5 — processed event skipped
        var user = await usersCtx.Users.FindAsync(2);
        user!.TokenBalance.Should().Be(5);
    }

    [Fact]
    public async Task ProcessPendingEvents_MaxRetriesExceeded_SkipsEvent()
    {
        var (scopeFactory, commsCtx, usersCtx) = BuildScopeFactory();

        usersCtx.Users.Add(MakeUser(userId: 3, tokens: 5));
        await usersCtx.SaveChangesAsync();

        // Event that has already been retried MaxRetries (3) times — should be skipped
        commsCtx.OutboxMessages.Add(new OutboxMessage
        {
            EventType  = "SuperLikeTokenDeduction",
            Payload    = JsonSerializer.Serialize(new { UserId = 3 }),
            CreatedAt  = DateTime.UtcNow,
            RetryCount = 3 // MaxRetries == 3, so RetryCount < MaxRetries is false
        });
        await commsCtx.SaveChangesAsync();

        var svc = CreateService(scopeFactory);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var executeTask = svc.StartAsync(cts.Token);
        await Task.Delay(200, CancellationToken.None);
        cts.Cancel();
        await svc.StopAsync(CancellationToken.None);

        var user = await usersCtx.Users.FindAsync(3);
        user!.TokenBalance.Should().Be(5); // untouched
    }

    // ─── Error handling ───────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessPendingEvents_UserNotFound_IncrementsRetryCountAndSetsError()
    {
        var (scopeFactory, commsCtx, usersCtx) = BuildScopeFactory();
        // Note: user NOT seeded

        commsCtx.OutboxMessages.Add(SuperLikeEvent(userId: 999));
        await commsCtx.SaveChangesAsync();

        var svc = CreateService(scopeFactory);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var executeTask = svc.StartAsync(cts.Token);
        await Task.Delay(200, CancellationToken.None);
        cts.Cancel();
        await svc.StopAsync(CancellationToken.None);

        var evt = await commsCtx.OutboxMessages.FirstAsync();
        evt.RetryCount.Should().Be(1);
        evt.Error.Should().NotBeNullOrEmpty();
        evt.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public async Task ProcessPendingEvents_InsufficientTokens_IncrementsRetryCount()
    {
        var (scopeFactory, commsCtx, usersCtx) = BuildScopeFactory();

        // User with 0 tokens — deduction should fail
        usersCtx.Users.Add(MakeUser(userId: 4, tokens: 0));
        await usersCtx.SaveChangesAsync();

        commsCtx.OutboxMessages.Add(SuperLikeEvent(userId: 4));
        await commsCtx.SaveChangesAsync();

        var svc = CreateService(scopeFactory);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var executeTask = svc.StartAsync(cts.Token);
        await Task.Delay(200, CancellationToken.None);
        cts.Cancel();
        await svc.StopAsync(CancellationToken.None);

        var evt = await commsCtx.OutboxMessages.FirstAsync();
        evt.RetryCount.Should().Be(1);
        evt.Error.Should().Contain("insufficient tokens");
        evt.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public async Task ProcessPendingEvents_UnknownEventType_IncrementsRetryCount()
    {
        var (scopeFactory, commsCtx, _) = BuildScopeFactory();

        commsCtx.OutboxMessages.Add(new OutboxMessage
        {
            EventType = "UnknownEventXYZ",
            Payload   = "{}",
            CreatedAt = DateTime.UtcNow
        });
        await commsCtx.SaveChangesAsync();

        var svc = CreateService(scopeFactory);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var executeTask = svc.StartAsync(cts.Token);
        await Task.Delay(200, CancellationToken.None);
        cts.Cancel();
        await svc.StopAsync(CancellationToken.None);

        var evt = await commsCtx.OutboxMessages.FirstAsync();
        evt.RetryCount.Should().Be(1);
        evt.Error.Should().Contain("Unknown outbox event type");
    }

    // ─── Multiple events ──────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessPendingEvents_MultipleEvents_ProcessesAll()
    {
        var (scopeFactory, commsCtx, usersCtx) = BuildScopeFactory();

        usersCtx.Users.AddRange(
            MakeUser(userId: 10, tokens: 5),
            MakeUser(userId: 11, tokens: 5));
        await usersCtx.SaveChangesAsync();

        commsCtx.OutboxMessages.AddRange(
            SuperLikeEvent(10),
            SuperLikeEvent(11));
        await commsCtx.SaveChangesAsync();

        var svc = CreateService(scopeFactory);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var executeTask = svc.StartAsync(cts.Token);
        await Task.Delay(300, CancellationToken.None);
        cts.Cancel();
        await svc.StopAsync(CancellationToken.None);

        usersCtx.ChangeTracker.Clear();
        (await usersCtx.Users.FindAsync(10))!.TokenBalance.Should().Be(4);
        (await usersCtx.Users.FindAsync(11))!.TokenBalance.Should().Be(4);

        var events = await commsCtx.OutboxMessages.ToListAsync();
        events.Should().OnlyContain(e => e.ProcessedAt != null);
    }

    // ─── No events ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessPendingEvents_NoEvents_CompletesWithoutError()
    {
        var (scopeFactory, _, _) = BuildScopeFactory();

        var svc = CreateService(scopeFactory);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var act = async () =>
        {
            var executeTask = svc.StartAsync(cts.Token);
            await Task.Delay(150, CancellationToken.None);
            cts.Cancel();
            await svc.StopAsync(CancellationToken.None);
        };

        await act.Should().NotThrowAsync();
    }
}
