namespace Lander.src.Modules.Payments.Dtos;

public class SubscriptionPlanDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "EUR";
    public string StripePriceId { get; set; } = string.Empty;
    public string Interval { get; set; } = "month";
}
