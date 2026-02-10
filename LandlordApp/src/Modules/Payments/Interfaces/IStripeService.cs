using Stripe.Checkout;

namespace Lander.src.Modules.Payments.Interfaces;

public interface IStripeService
{
    Task<Session> CreateCheckoutSessionAsync(string priceId, string successUrl, string cancelUrl, int userId);
    Task HandleWebhookAsync(string json, string signature);
}
