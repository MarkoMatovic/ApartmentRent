using FluentAssertions;
using Lander;
using Lander.src.Modules.Payments;
using Lander.src.Modules.Payments.Interfaces;
using LandlordApp.Tests.E2eTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LandlordApp.Tests.E2eTests.Tests;

/// <summary>
/// Runs payment fulfilment and reversal against a REAL SQL Server (Testcontainers). The
/// guarantees under test — a unique-key claim and atomic UPDATEs — only exist in a real
/// relational engine; the InMemory provider cannot execute the raw SQL or ExecuteUpdate.
/// </summary>
[Collection(E2eCollection.Name)]
public class PaymentFulfillmentE2eTests : E2eTestBase
{
    public PaymentFulfillmentE2eTests(E2eFixture fixture) : base(fixture) { }

    // Each concurrent caller needs its own scope: DbContext instances are not thread-safe.
    private IServiceScope NewScope() => Fixture.Factory.Services.CreateScope();

    private async Task<int> TokenBalanceAsync(int userId)
    {
        using var scope = NewScope();
        return await scope.ServiceProvider.GetRequiredService<UsersContext>().Users
            .AsNoTracking().Where(u => u.UserId == userId).Select(u => u.TokenBalance).SingleAsync();
    }

    private async Task SetTokenBalanceAsync(int userId, int balance)
    {
        using var scope = NewScope();
        await scope.ServiceProvider.GetRequiredService<UsersContext>().Database.ExecuteSqlRawAsync(
            "UPDATE [UsersRoles].[Users] SET TokenBalance = {0} WHERE UserId = {1}", balance, userId);
    }

    [Fact]
    public async Task Fulfill_ConcurrentDuplicateWebhooks_GrantsExactlyOnce()
    {
        var user = await Data.CreateUserAsync("race@test.com");
        await SetTokenBalanceAsync(user.UserId, 0);

        // Paddle sends transaction.paid and transaction.completed (and retries) back to back, so
        // several webhooks for ONE transaction can be in flight at the same instant.
        var tasks = Enumerable.Range(0, 8).Select(async _ =>
        {
            using var scope = NewScope();
            var fulfilment = scope.ServiceProvider.GetRequiredService<IPaymentFulfillmentService>();
            await fulfilment.FulfillAsync("txn_race_1", user.UserId, "tokens-50");
        });
        await Task.WhenAll(tasks);

        (await TokenBalanceAsync(user.UserId)).Should().Be(50, "50 tokens were bought once, however many webhooks arrived");

        using var check = NewScope();
        (await check.ServiceProvider.GetRequiredService<PaymentsContext>().ProcessedOrders
            .CountAsync(o => o.OrderNumber == "txn_race_1")).Should().Be(1);
    }

    [Fact]
    public async Task Fulfill_RecordsBuyerAndPlanOnTheOrder()
    {
        var user = await Data.CreateUserAsync("record@test.com");
        using var scope = NewScope();

        await scope.ServiceProvider.GetRequiredService<IPaymentFulfillmentService>()
            .FulfillAsync("txn_record_1", user.UserId, "tokens-10");

        var order = await scope.ServiceProvider.GetRequiredService<PaymentsContext>().ProcessedOrders
            .AsNoTracking().SingleAsync(o => o.OrderNumber == "txn_record_1");
        order.UserId.Should().Be(user.UserId);
        order.PlanId.Should().Be("tokens-10");
        order.ReversedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetUserOrders_ListsPaddlePurchases()
    {
        var user = await Data.CreateUserAsync("history@test.com");
        using var scope = NewScope();
        await scope.ServiceProvider.GetRequiredService<IPaymentFulfillmentService>()
            .FulfillAsync("txn_history_1", user.UserId, "tokens-50");

        var orders = await scope.ServiceProvider.GetRequiredService<IPaymentService>().GetUserOrdersAsync(user.UserId);

        // Paddle order numbers are "txn_…", which never matched the old "{userId}_" prefix filter.
        orders.Should().ContainSingle(o => o.OrderNumber == "txn_history_1" && o.PlanId == "tokens-50");
    }

    [Fact]
    public async Task Reverse_FullRefund_TakesTokensBack_AndIsIdempotent()
    {
        var user = await Data.CreateUserAsync("refund@test.com");
        await SetTokenBalanceAsync(user.UserId, 0);
        using (var s = NewScope())
            await s.ServiceProvider.GetRequiredService<IPaymentFulfillmentService>()
                .FulfillAsync("txn_refund_1", user.UserId, "tokens-50");
        (await TokenBalanceAsync(user.UserId)).Should().Be(50);

        using var scope = NewScope();
        var reversal = scope.ServiceProvider.GetRequiredService<IPaymentReversalService>();

        (await reversal.ReverseAsync("txn_refund_1", "adj_1", isChargeback: false)).Should().Be(ReversalOutcome.Reversed);
        (await TokenBalanceAsync(user.UserId)).Should().Be(0);

        // Paddle retries / adjustment.created + adjustment.updated must not subtract twice.
        (await reversal.ReverseAsync("txn_refund_1", "adj_1", isChargeback: false)).Should().Be(ReversalOutcome.AlreadyReversed);
        (await TokenBalanceAsync(user.UserId)).Should().Be(0);
    }

    [Fact]
    public async Task Reverse_ConcurrentDuplicateAdjustments_TakesBackOnce()
    {
        var user = await Data.CreateUserAsync("refundrace@test.com");
        await SetTokenBalanceAsync(user.UserId, 100); // 50 bought on top of 50 owned
        using (var s = NewScope())
            await s.ServiceProvider.GetRequiredService<IPaymentFulfillmentService>()
                .FulfillAsync("txn_refund_race", user.UserId, "tokens-50");
        (await TokenBalanceAsync(user.UserId)).Should().Be(150);

        await Task.WhenAll(Enumerable.Range(0, 6).Select(async _ =>
        {
            using var scope = NewScope();
            await scope.ServiceProvider.GetRequiredService<IPaymentReversalService>()
                .ReverseAsync("txn_refund_race", "adj_race", isChargeback: false);
        }));

        (await TokenBalanceAsync(user.UserId)).Should().Be(100, "exactly one reversal of 50 tokens");
    }

    [Fact]
    public async Task Reverse_NeverDrivesTokenBalanceBelowZero()
    {
        var user = await Data.CreateUserAsync("spent@test.com");
        using (var s = NewScope())
            await s.ServiceProvider.GetRequiredService<IPaymentFulfillmentService>()
                .FulfillAsync("txn_spent_1", user.UserId, "tokens-50");

        // The buyer already spent most of the tokens.
        await SetTokenBalanceAsync(user.UserId, 10);

        using var scope = NewScope();
        await scope.ServiceProvider.GetRequiredService<IPaymentReversalService>()
            .ReverseAsync("txn_spent_1", "adj_spent", isChargeback: true);

        (await TokenBalanceAsync(user.UserId)).Should().Be(0);
    }

    [Fact]
    public async Task Reverse_UnknownTransaction_ReportsNotFound()
    {
        using var scope = NewScope();
        var outcome = await scope.ServiceProvider.GetRequiredService<IPaymentReversalService>()
            .ReverseAsync("txn_never_fulfilled", "adj_x", isChargeback: false);

        outcome.Should().Be(ReversalOutcome.OrderNotFound);
    }
}
