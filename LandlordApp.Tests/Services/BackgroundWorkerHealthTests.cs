using FluentAssertions;
using Lander.src.Infrastructure.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace LandlordApp.Tests.Services;

public class BackgroundWorkerHealthTests
{
    private static readonly Exception Boom = new InvalidOperationException("boom");

    private static async Task<HealthCheckResult> CheckAsync(BackgroundWorkerHealth h) =>
        await new BackgroundWorkersHealthCheck(h).CheckHealthAsync(new HealthCheckContext());

    [Fact]
    public async Task NoWorkersReported_IsHealthy()
    {
        (await CheckAsync(new BackgroundWorkerHealth(TimeProvider.System))).Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task FewFailures_BelowThreshold_StayHealthy()
    {
        var h = new BackgroundWorkerHealth(TimeProvider.System);
        for (var i = 0; i < BackgroundWorkerHealth.FailureThreshold - 1; i++)
            h.RecordFailure("outbox", Boom);

        (await CheckAsync(h)).Status.Should().Be(HealthStatus.Healthy, "a single blip is not an incident");
    }

    [Fact]
    public async Task RepeatedFailures_ReportDegraded_WithTheWorkerAndError()
    {
        var h = new BackgroundWorkerHealth(TimeProvider.System);
        for (var i = 0; i < BackgroundWorkerHealth.FailureThreshold; i++)
            h.RecordFailure("outbox", Boom);

        var result = await CheckAsync(h);

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("outbox");
        result.Data["outbox"].ToString().Should().Contain("boom");
    }

    [Fact]
    public async Task ASuccess_ResetsTheStreak_AndRecovers()
    {
        var h = new BackgroundWorkerHealth(TimeProvider.System);
        for (var i = 0; i < 10; i++) h.RecordFailure("outbox", Boom);
        (await CheckAsync(h)).Status.Should().Be(HealthStatus.Degraded);

        h.RecordSuccess("outbox");

        (await CheckAsync(h)).Status.Should().Be(HealthStatus.Healthy);
        h.RecordFailure("outbox", Boom).Should().Be(1, "the streak restarts from zero after a success");
    }

    [Fact]
    public async Task OneWorkerFailing_DoesNotMaskAnother()
    {
        var h = new BackgroundWorkerHealth(TimeProvider.System);
        for (var i = 0; i < 5; i++) h.RecordFailure("outbox", Boom);
        h.RecordSuccess("other");

        var result = await CheckAsync(h);

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Data.Keys.Should().ContainSingle().Which.Should().Be("outbox");
    }
}
