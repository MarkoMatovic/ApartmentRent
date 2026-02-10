using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;
using Lander.src.Modules.Payments.Interfaces;
using Lander.src.Modules.Users.Interfaces.UserInterface;

namespace Lander.src.Modules.Payments.Implementation;

public class StripeService : IStripeService
{
    private readonly string _webhookSecret;
    private readonly IUserInterface _userService;

    public StripeService(IConfiguration configuration, IUserInterface userService)
    {
        StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];
        _webhookSecret = configuration["Stripe:WebhookSecret"];
        _userService = userService;
    }

    public async Task<Session> CreateCheckoutSessionAsync(string priceId, string successUrl, string cancelUrl, int userId)
    {
        var options = new SessionCreateOptions
        {
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    Price = priceId,
                    Quantity = 1,
                },
            },
            Mode = "subscription",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                { "userId", userId.ToString() }
            }
        };

        var service = new SessionService();
        return await service.CreateAsync(options);
    }

    public async Task HandleWebhookAsync(string json, string signature)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(json, signature, _webhookSecret);

            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;
                
                if (session.Metadata.TryGetValue("userId", out var userIdStr) && int.TryParse(userIdStr, out var userId))
                {
                    // Get user's current profile to check role
                    var userProfile = await _userService.GetUserProfileAsync(userId);
                    if (userProfile != null)
                    {
                        // Determine target premium role based on current role
                        string targetRoleName = userProfile.RoleName switch
                        {
                            "Tenant" => "Premium Tenant",
                            "TenantLandlord" => "Premium Landlord",
                            // If already premium or other role, keep Premium Landlord as default
                            _ => "Premium Landlord"
                        };

                        // Update user role via UserService
                        await _userService.UpgradeUserRoleAsync(userId, targetRoleName);
                        
                        // Also update the analytics flag for backward compatibility
                        var updateDto = new Lander.src.Modules.Users.Dtos.InputDto.UserProfileUpdateInputDto
                        {
                            FirstName = userProfile.FirstName,
                            LastName = userProfile.LastName,
                            Email = userProfile.Email,
                            PhoneNumber = userProfile.PhoneNumber,
                            DateOfBirth = userProfile.DateOfBirth,
                            HasPersonalAnalytics = true // Grant premium access flag
                        };
                        await _userService.UpdateUserProfileAsync(userId, updateDto);
                    }
                }
            }
            else if (stripeEvent.Type == "customer.subscription.deleted")
            {
                // Handle subscription cancellation - downgrade user
                var subscription = stripeEvent.Data.Object as Stripe.Subscription;
                
                if (subscription?.Metadata.TryGetValue("userId", out var userIdStr) == true && 
                    int.TryParse(userIdStr, out var userId))
                {
                    var userProfile = await _userService.GetUserProfileAsync(userId);
                    if (userProfile != null)
                    {
                        // Determine downgrade target
                        string downgradedRoleName = userProfile.RoleName switch
                        {
                            "Premium Tenant" => "Tenant",
                            "Premium Landlord" => "TenantLandlord",
                            _ => userProfile.RoleName // Keep current if not a premium role
                        };

                        // Downgrade user role
                        await _userService.UpgradeUserRoleAsync(userId, downgradedRoleName);
                        
                        // Remove analytics flag
                        var updateDto = new Lander.src.Modules.Users.Dtos.InputDto.UserProfileUpdateInputDto
                        {
                            FirstName = userProfile.FirstName,
                            LastName = userProfile.LastName,
                            Email = userProfile.Email,
                            PhoneNumber = userProfile.PhoneNumber,
                            DateOfBirth = userProfile.DateOfBirth,
                            HasPersonalAnalytics = false // Remove premium access
                        };
                        await _userService.UpdateUserProfileAsync(userId, updateDto);
                    }
                }
            }
        }
        catch (StripeException e)
        {
            // Log error
            throw new Exception($"Webhook Error: {e.Message}");
        }
    }
}
