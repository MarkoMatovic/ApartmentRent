namespace Lander.src.Modules.Payments.Dtos;

/// <summary>Current state of a user's purchased features — returned by GET /api/payments/my-status.</summary>
public class UserSubscriptionStatusDto
{
    public bool HasAnalytics { get; set; }
    public bool IsAnalyticsActive => HasAnalytics;
    public int TokenBalance { get; set; }
    public int ListingCredits { get; set; }
    public DateTime? BoostedUntil { get; set; }
    public DateTime? PriorityInboxUntil { get; set; }
    public string? RoleName { get; set; }
    public bool IsBoosted => BoostedUntil.HasValue && BoostedUntil > DateTime.UtcNow;
    public bool HasPriorityInbox => PriorityInboxUntil.HasValue && PriorityInboxUntil > DateTime.UtcNow;
}

/// <summary>One entry in the user's payment history — returned by GET /api/payments/my-orders.</summary>
public class PaymentOrderDto
{
    public string OrderNumber { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public DateTimeOffset ProcessedAt { get; set; }
}
