using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Lander.src.Modules.Payments.Implementation;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Lander.src.Modules.Users.Dtos.Dto;

namespace LandlordApp.Tests.Services;

public class PaymentServiceTests
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<IUserInterface> _mockUserService;
    private readonly StripeService _service;

    public PaymentServiceTests()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(c => c["Stripe:SecretKey"]).Returns("sk_test_123");
        _mockConfig.Setup(c => c["Stripe:WebhookSecret"]).Returns("whsec_123");

        _mockUserService = new Mock<IUserInterface>();

        _service = new StripeService(_mockConfig.Object, _mockUserService.Object);
    }

    [Fact]
    public void Constructor_ShouldSucceed()
    {
        // Assert
        _service.Should().NotBeNull();
    }

    // Note: Full testing of HandleWebhookAsync and CreateCheckoutSessionAsync 
    // requires wrapping Stripe's static/service classes to allow mocking,
    // or using integration tests with a Stripe CLI/Fixture.
    
    // We can at least verify that it attempts to use the UserInterface
    // if we could get past the Stripe event validation.
}
