using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using Lander.Helpers;
using Lander.src.Modules.Payments.Implementation;
using Lander.src.Modules.Payments.Interfaces;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Lander.src.Modules.Users.Dtos.Dto;

namespace LandlordApp.Tests.Services;

public class MonriServiceTests
{
    private const string MerchantKey = "test_merchant_key_1234";
    private const string AuthenticityToken = "test_authenticity_token";

    private readonly Mock<IUserInterface> _mockUserService;
    private readonly MonriService _service;
    private readonly IConfiguration _config;

    public MonriServiceTests()
    {
        _mockUserService = new Mock<IUserInterface>();

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Monri:AuthenticityToken"] = AuthenticityToken,
                ["Monri:MerchantKey"] = MerchantKey,
                ["Monri:BaseUrl"] = "https://ipgtest.monri.com",
                ["Monri:CallbackBaseUrl"] = "https://localhost:7092",
                ["Monri:Plans:basic:Name"] = "Basic",
                ["Monri:Plans:basic:Amount"] = "999",
                ["Monri:Plans:basic:Currency"] = "EUR",
            })
            .Build();

        var formService = new MonriPaymentFormService(
            _config,
            new Mock<ILogger<MonriPaymentFormService>>().Object);

        var callbackHandler = new MonriCallbackHandler(
            _config,
            _mockUserService.Object,
            new Mock<ILogger<MonriCallbackHandler>>().Object,
            TimeProvider.System);

        var idempotencyService = new IdempotencyService(
            new Mock<IDistributedCache>().Object);

        _service = new MonriService(
            formService,
            callbackHandler,
            _mockUserService.Object,
            idempotencyService,
            _config);
    }

    // ─── HandleCallbackAsync — JSON validation ────────────────────────────────

    [Fact]
    public async Task HandleCallbackAsync_InvalidJson_ThrowsArgumentException()
    {
        var act = async () => await _service.HandleCallbackAsync("not-valid-json{{{");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task HandleCallbackAsync_EmptyJson_DoesNotThrow()
    {
        // Empty object — no event field, no pgw_response_code → nothing to process
        var act = async () => await _service.HandleCallbackAsync("{}");
        await act.Should().NotThrowAsync();
    }

    // ─── HandleCallbackAsync — digest validation ──────────────────────────────

    [Fact]
    public async Task HandleCallbackAsync_CorrectDigest_ProcessesTransaction()
    {
        var orderNumber = $"1_basic_{Guid.NewGuid():N}";

        _mockUserService
            .Setup(u => u.GetUserProfileAsync(1))
            .ReturnsAsync(new UserProfileDto { UserId = 1, RoleName = "Tenant", FirstName = "A", LastName = "B", Email = "a@b.com" });
        _mockUserService
            .Setup(u => u.UpgradeUserRoleAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _mockUserService
            .Setup(u => u.UpdateUserProfileAsync(It.IsAny<int>(), It.IsAny<Lander.src.Modules.Users.Dtos.InputDto.UserProfileUpdateInputDto>()))
            .ReturnsAsync(new UserProfileDto());

        var digest = ComputeCallbackDigest(
            MerchantKey, orderNumber,
            pgwTransactionId: "txn001",
            pgwResponseCode: "0000",
            pgwAmount: "999",
            pgwOutgoingAmount: "999",
            pgwCurrency: "EUR",
            pgwOutgoingCurrency: "EUR",
            pgwApprovalCode: "APP01",
            pgwResponseMessage: "Approved");

        var json = $$"""
            {
                "pgw_response_code": "0000",
                "order_number": "{{orderNumber}}",
                "pgw_transaction_id": "txn001",
                "pgw_amount": "999",
                "pgw_outgoing_amount": "999",
                "pgw_currency": "EUR",
                "pgw_outgoing_currency": "EUR",
                "pgw_approval_code": "APP01",
                "pgw_response_message": "Approved",
                "digest": "{{digest}}"
            }
            """;

        await _service.HandleCallbackAsync(json);

        _mockUserService.Verify(u => u.UpgradeUserRoleAsync(1, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task HandleCallbackAsync_WrongDigest_ThrowsInvalidOperationException()
    {
        var orderNumber = $"1_basic_{Guid.NewGuid():N}";

        var json = $$"""
            {
                "pgw_response_code": "0000",
                "order_number": "{{orderNumber}}",
                "pgw_transaction_id": "txn001",
                "pgw_amount": "999",
                "pgw_outgoing_amount": "999",
                "pgw_currency": "EUR",
                "pgw_outgoing_currency": "EUR",
                "pgw_approval_code": "APP01",
                "pgw_response_message": "Approved",
                "digest": "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000"
            }
            """;

        var act = async () => await _service.HandleCallbackAsync(json);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*digest*");
    }

    // ─── Idempotency ──────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleCallbackAsync_DuplicateOrderNumber_ProcessedOnlyOnce()
    {
        var orderNumber = $"1_basic_{Guid.NewGuid():N}";

        _mockUserService
            .Setup(u => u.GetUserProfileAsync(1))
            .ReturnsAsync(new UserProfileDto { UserId = 1, RoleName = "Tenant", FirstName = "A", LastName = "B", Email = "a@b.com" });
        _mockUserService
            .Setup(u => u.UpgradeUserRoleAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _mockUserService
            .Setup(u => u.UpdateUserProfileAsync(It.IsAny<int>(), It.IsAny<Lander.src.Modules.Users.Dtos.InputDto.UserProfileUpdateInputDto>()))
            .ReturnsAsync(new UserProfileDto());

        var digest = ComputeCallbackDigest(
            MerchantKey, orderNumber,
            "txn001", "0000", "999", "999", "EUR", "EUR", "APP01", "Approved");

        var json = $$"""
            {
                "pgw_response_code": "0000",
                "order_number": "{{orderNumber}}",
                "pgw_transaction_id": "txn001",
                "pgw_amount": "999",
                "pgw_outgoing_amount": "999",
                "pgw_currency": "EUR",
                "pgw_outgoing_currency": "EUR",
                "pgw_approval_code": "APP01",
                "pgw_response_message": "Approved",
                "digest": "{{digest}}"
            }
            """;

        await _service.HandleCallbackAsync(json);
        await _service.HandleCallbackAsync(json); // duplicate

        // UpgradeUserRole must be called exactly once despite two callbacks
        _mockUserService.Verify(u => u.UpgradeUserRoleAsync(1, It.IsAny<string>()), Times.Once);
    }

    // ─── CreatePaymentForm ────────────────────────────────────────────────────

    [Fact]
    public void CreatePaymentForm_ValidPlan_ReturnsFormDto()
    {
        var result = _service.CreatePaymentForm(
            "basic", "https://example.com/success", "https://example.com/failure",
            42, "buyer@test.com", "Buyer Name");

        result.Should().NotBeNull();
        result.Amount.Should().Be(999);
        result.Currency.Should().Be("EUR");
        result.AuthenticityToken.Should().Be(AuthenticityToken);
        result.BuyerEmail.Should().Be("buyer@test.com");
        result.OrderNumber.Should().StartWith("42_basic_");
        result.Digest.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreatePaymentForm_InvalidPlan_ThrowsArgumentException()
    {
        var act = () => _service.CreatePaymentForm(
            "nonexistent_plan", "https://s.com", "https://f.com",
            1, "e@e.com", "Name");

        act.Should().Throw<ArgumentException>().WithMessage("*nonexistent_plan*");
    }

    [Fact]
    public void CreatePaymentForm_DigestIsCorrectSha512()
    {
        var result = _service.CreatePaymentForm(
            "basic", "https://s.com", "https://f.com",
            1, "e@e.com", "Name");

        // Recompute digest: SHA512(merchantKey + orderNumber + amount + currency)
        var data = $"{MerchantKey}{result.OrderNumber}{result.Amount}{result.Currency}";
        var expected = Convert.ToHexString(SHA512.HashData(Encoding.UTF8.GetBytes(data))).ToLower();

        result.Digest.Should().Be(expected);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static string ComputeCallbackDigest(
        string merchantKey,
        string orderNumber,
        string pgwTransactionId,
        string pgwResponseCode,
        string pgwAmount,
        string pgwOutgoingAmount,
        string pgwCurrency,
        string pgwOutgoingCurrency,
        string pgwApprovalCode,
        string pgwResponseMessage)
    {
        var raw = string.Concat(
            merchantKey, orderNumber, pgwTransactionId,
            pgwResponseCode, pgwAmount, pgwOutgoingAmount,
            pgwCurrency, pgwOutgoingCurrency,
            pgwApprovalCode, pgwResponseMessage);
        return Convert.ToHexString(SHA512.HashData(Encoding.UTF8.GetBytes(raw))).ToLower();
    }
}
