using System.Security.Claims;
using FluentAssertions;
using Lander.src.Infrastructure.Services;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace LandlordApp.Tests.Services;

public class PresenceTrackerTests
{
    private readonly PresenceTracker _tracker = new();

    [Fact]
    public void Unknown_user_is_offline()
    {
        _tracker.IsOnline(1).Should().BeFalse();
    }

    [Fact]
    public void A_connected_user_is_online_until_the_connection_closes()
    {
        _tracker.Connected(1, "conn-a");
        _tracker.IsOnline(1).Should().BeTrue();

        _tracker.Disconnected("conn-a");
        _tracker.IsOnline(1).Should().BeFalse();
    }

    [Fact]
    public void Several_tabs_keep_the_user_online_until_the_last_one_closes()
    {
        _tracker.Connected(1, "chat-tab");
        _tracker.Connected(1, "notification-tab");

        _tracker.Disconnected("chat-tab");
        _tracker.IsOnline(1).Should().BeTrue();

        _tracker.Disconnected("notification-tab");
        _tracker.IsOnline(1).Should().BeFalse();
    }

    [Fact]
    public void Users_are_tracked_independently()
    {
        _tracker.Connected(1, "a");
        _tracker.Connected(2, "b");

        _tracker.Disconnected("a");

        _tracker.IsOnline(1).Should().BeFalse();
        _tracker.IsOnline(2).Should().BeTrue();
    }

    [Fact]
    public void Disconnecting_an_unknown_connection_is_harmless()
    {
        var act = () => _tracker.Disconnected("never-seen");

        act.Should().NotThrow();
    }

    [Fact]
    public void A_connection_reported_twice_counts_once()
    {
        _tracker.Connected(1, "a");
        _tracker.Connected(1, "a");

        _tracker.Disconnected("a");

        _tracker.IsOnline(1).Should().BeFalse();
    }
}

public class PresenceHubFilterTests
{
    private static HubLifetimeContext ContextFor(string? userId, string connectionId)
    {
        var caller = new Mock<HubCallerContext>();
        caller.Setup(c => c.ConnectionId).Returns(connectionId);
        var claims = userId is null ? [] : new List<Claim> { new("userId", userId) };
        caller.Setup(c => c.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(claims, "test")));
        return new HubLifetimeContext(caller.Object, new Mock<IServiceProvider>().Object, new Mock<Hub>().Object);
    }

    [Fact]
    public async Task Connecting_marks_the_user_online_and_calls_the_hub()
    {
        var tracker = new PresenceTracker();
        var filter = new PresenceHubFilter(tracker);
        var nextCalled = false;

        await filter.OnConnectedAsync(ContextFor("7", "c1"), _ => { nextCalled = true; return Task.CompletedTask; });

        tracker.IsOnline(7).Should().BeTrue();
        nextCalled.Should().BeTrue("the filter must not swallow the hub's own OnConnectedAsync");
    }

    [Fact]
    public async Task Disconnecting_marks_the_user_offline_and_calls_the_hub()
    {
        var tracker = new PresenceTracker();
        var filter = new PresenceHubFilter(tracker);
        var context = ContextFor("7", "c1");
        await filter.OnConnectedAsync(context, _ => Task.CompletedTask);
        var nextCalled = false;

        await filter.OnDisconnectedAsync(context, null, (_, _) => { nextCalled = true; return Task.CompletedTask; });

        tracker.IsOnline(7).Should().BeFalse();
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task A_connection_without_a_user_id_claim_is_ignored()
    {
        var tracker = new PresenceTracker();
        var filter = new PresenceHubFilter(tracker);

        await filter.OnConnectedAsync(ContextFor(null, "c1"), _ => Task.CompletedTask);

        tracker.IsOnline(0).Should().BeFalse();
    }
}
