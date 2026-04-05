using System;

namespace Lander.src.Modules.Payments.Models
{
    public class Subscription
    {
        public int SubscriptionId { get; set; }
        public Guid SubscriptionGuid { get; set; }
        public int UserId { get; set; } // Foreign key to Users module
        public string PlanType { get; set; } = string.Empty; // "Monthly", "Yearly"
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public string? ExternalSubscriptionId { get; set; } // ID from Payten if applicable
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
