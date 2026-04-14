using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Lander.src.Modules.Payments;
using Lander.src.Modules.Payments.Implementation;
using Lander.src.Modules.Payments.Models;

namespace LandlordApp.Tests.Services;

public class PaytenPaymentServiceTests : IDisposable
{
    private readonly PaymentsContext _context;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly PaytenPaymentService _service;

    public PaytenPaymentServiceTests()
    {
        var options = new DbContextOptionsBuilder<PaymentsContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new PaymentsContext(options);

        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(c => c["Payten:MerchantId"]).Returns("test_merchant");
        _mockConfig.Setup(c => c["Payten:SecretKey"]).Returns("test_secret");
        _mockConfig.Setup(c => c["Payten:BaseUrl"]).Returns("https://checkout.sandbox.payten.com");

        _service = new PaytenPaymentService(_context, _mockConfig.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    // ─── InitiateCheckoutAsync ────────────────────────────────────────────────

    [Fact]
    public async Task InitiateCheckoutAsync_ValidInput_CreatesTransactionInDb()
    {
        await _service.InitiateCheckoutAsync(userId: 1, planType: "Monthly", amount: 9.99m);

        var transaction = await _context.Transactions.FirstOrDefaultAsync();
        transaction.Should().NotBeNull();
        transaction!.UserId.Should().Be(1);
        transaction.Amount.Should().Be(9.99m);
        transaction.Status.Should().Be("Pending");
        transaction.OrderDescription.Should().Contain("Monthly");
    }

    [Fact]
    public async Task InitiateCheckoutAsync_ReturnsCheckoutUrl()
    {
        var url = await _service.InitiateCheckoutAsync(userId: 2, planType: "Yearly", amount: 99.99m);

        url.Should().NotBeNullOrWhiteSpace();
        url.Should().StartWith("https://");
    }

    // ─── ProcessWebhookAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task ProcessWebhookAsync_UnknownTransactionId_ReturnsFalse()
    {
        var result = await _service.ProcessWebhookAsync("nonexistent-tx-id", "Success");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessWebhookAsync_SuccessStatus_ActivatesSubscription()
    {
        var transaction = new Transaction
        {
            UserId = 10,
            Amount = 9.99m,
            Status = "Pending",
            OrderDescription = "Subscription: Monthly",
            PaytenTransactionId = "payten-tx-001",
            TransactionGuid = Guid.NewGuid()
        };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        var result = await _service.ProcessWebhookAsync("payten-tx-001", "Success");

        result.Should().BeTrue();

        var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s => s.UserId == 10);
        subscription.Should().NotBeNull();
        subscription!.IsActive.Should().BeTrue();
        subscription.PlanType.Should().Be("Monthly");
        subscription.EndDate.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task ProcessWebhookAsync_SuccessStatus_UpdatesExistingSubscription()
    {
        var transaction = new Transaction
        {
            UserId = 20,
            Amount = 99.99m,
            Status = "Pending",
            OrderDescription = "Subscription: Yearly",
            PaytenTransactionId = "payten-tx-002",
            TransactionGuid = Guid.NewGuid()
        };
        var existingSub = new Subscription
        {
            UserId = 20,
            PlanType = "Monthly",
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow.AddDays(5),
            IsActive = true,
            SubscriptionGuid = Guid.NewGuid()
        };
        _context.Transactions.Add(transaction);
        _context.Subscriptions.Add(existingSub);
        await _context.SaveChangesAsync();

        var result = await _service.ProcessWebhookAsync("payten-tx-002", "Success");

        result.Should().BeTrue();

        var subscriptions = await _context.Subscriptions.Where(s => s.UserId == 20).ToListAsync();
        subscriptions.Should().HaveCount(1);

        var updated = subscriptions.First();
        updated.PlanType.Should().Be("Yearly");
        updated.IsActive.Should().BeTrue();
        updated.EndDate.Should().BeAfter(DateTime.UtcNow.AddMonths(6));
    }

    [Fact]
    public async Task ProcessWebhookAsync_NonSuccessStatus_DoesNotCreateSubscription()
    {
        var transaction = new Transaction
        {
            UserId = 30,
            Amount = 9.99m,
            Status = "Pending",
            OrderDescription = "Subscription: Monthly",
            PaytenTransactionId = "payten-tx-003",
            TransactionGuid = Guid.NewGuid()
        };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        var result = await _service.ProcessWebhookAsync("payten-tx-003", "Failed");

        result.Should().BeTrue();

        var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s => s.UserId == 30);
        subscription.Should().BeNull();
    }

    // ─── GetActiveSubscriptionAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetActiveSubscriptionAsync_ActiveSub_ReturnsIt()
    {
        var sub = new Subscription
        {
            UserId = 50,
            PlanType = "Monthly",
            StartDate = DateTime.UtcNow.AddDays(-10),
            EndDate = DateTime.UtcNow.AddDays(20),
            IsActive = true,
            SubscriptionGuid = Guid.NewGuid()
        };
        _context.Subscriptions.Add(sub);
        await _context.SaveChangesAsync();

        var result = await _service.GetActiveSubscriptionAsync(50);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(50);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetActiveSubscriptionAsync_NoSub_ReturnsNull()
    {
        var result = await _service.GetActiveSubscriptionAsync(9999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveSubscriptionAsync_ExpiredSub_ReturnsNull()
    {
        var expiredSub = new Subscription
        {
            UserId = 60,
            PlanType = "Monthly",
            StartDate = DateTime.UtcNow.AddMonths(-2),
            EndDate = DateTime.UtcNow.AddDays(-5),
            IsActive = true,
            SubscriptionGuid = Guid.NewGuid()
        };
        _context.Subscriptions.Add(expiredSub);
        await _context.SaveChangesAsync();

        var result = await _service.GetActiveSubscriptionAsync(60);

        result.Should().BeNull();
    }
}
