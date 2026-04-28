using Lander.src.Common;

namespace Lander.src.Modules.Payments.Models
{
    public class Transaction
    {
        public int TransactionId { get; set; }
        public Guid TransactionGuid { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EUR"; // or USD/RSD
        public string Status { get; set; } = ApplicationStatuses.Pending;
        public string? PaytenTransactionId { get; set; }
        public string? PaymentMethod { get; set; }
        public string OrderDescription { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
