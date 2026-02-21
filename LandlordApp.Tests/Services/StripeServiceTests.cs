using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Lander.src.Modules.Payments.Implementation;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Lander.src.Modules.Users.Dtos.Dto;
using Lander.src.Modules.Users.Dtos.InputDto;
using Stripe;
using System.Text.Json;

namespace LandlordApp.Tests.Services;

public class StripeServiceTests
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<IUserInterface> _mockUserService;
    private readonly StripeService _service;

    public StripeServiceTests()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(x => x["Stripe:SecretKey"]).Returns("sk_test_mock");
        _mockConfig.Setup(x => x["Stripe:WebhookSecret"]).Returns("whsec_test_mock");

        _mockUserService = new Mock<IUserInterface>();
        _service = new StripeService(_mockConfig.Object, _mockUserService.Object);
    }

    [Fact]
    public async Task HandleWebhookAsync_InvalidSignature_ShouldThrowException()
    {
        // Act
        var act = async () => await _service.HandleWebhookAsync("{}", "invalid_sig");

        // Assert - Stripe's EventUtility will throw if it can't verify
        await act.Should().ThrowAsync<Exception>().WithMessage("*Webhook Error*");
    }

    // Note: To test actual successful webhook logic, normally you'd need to mock EventUtility.ConstructEvent.
    // However, since it's a static class in Stripe.net, it's hard to mock without a wrapper.
    // In this specific implementation, HandleWebhookAsync calls EventUtility directly.
    // For this demonstration, we'll verify the signature check and role determination logic 
    // if we were able to refactor or use a wrapper.
}
