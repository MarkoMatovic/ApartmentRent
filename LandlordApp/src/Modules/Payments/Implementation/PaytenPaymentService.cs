using System;
using System.Threading.Tasks;
using Lander.src.Modules.Payments.Interfaces;
using Lander.src.Modules.Payments.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Lander.src.Modules.Payments.Implementation
{
    public class PaytenPaymentService : IPaymentService
    {
        private readonly PaymentsContext _context;
        private readonly IConfiguration _configuration;

        public PaytenPaymentService(PaymentsContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<string> InitiateCheckoutAsync(int userId, string planType, decimal amount)
        {
            // 1. Log transaction in DB as pending
            var transaction = new Transaction
            {
                UserId = userId,
                Amount = amount,
                Status = "Pending",
                OrderDescription = $"Subscription: {planType}"
            };
            
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            // 2. Build Payten API request here using _configuration["Payten:MerchantId"], etc.
            // For now, return a mock checkout URL for testing
            string mockCheckoutUrl = $"https://checkout.sandbox.payten.com/hpp/{Guid.NewGuid()}?tx={transaction.TransactionGuid}";

            return mockCheckoutUrl;
        }

        public async Task<bool> ProcessWebhookAsync(string paytenTransactionId, string status)
        {
            // 1. Find transaction
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.PaytenTransactionId == paytenTransactionId);

            if (transaction == null) return false;

            // 2. Update status
            transaction.Status = status;

            // 3. If success, activate subscription
            if (status == "Success")
            {
                // Parse plan from description (mock)
                string planType = transaction.OrderDescription.Contains("Yearly") ? "Yearly" : "Monthly";
                
                var existingSub = await _context.Subscriptions
                    .FirstOrDefaultAsync(s => s.UserId == transaction.UserId);

                if (existingSub != null)
                {
                    existingSub.IsActive = true;
                    existingSub.EndDate = planType == "Yearly" ? DateTime.UtcNow.AddYears(1) : DateTime.UtcNow.AddMonths(1);
                    existingSub.PlanType = planType;
                    _context.Subscriptions.Update(existingSub);
                }
                else
                {
                    var subscription = new Subscription
                    {
                        UserId = transaction.UserId,
                        PlanType = planType,
                        StartDate = DateTime.UtcNow,
                        EndDate = planType == "Yearly" ? DateTime.UtcNow.AddYears(1) : DateTime.UtcNow.AddMonths(1),
                        IsActive = true
                    };
                    _context.Subscriptions.Add(subscription);
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Subscription?> GetActiveSubscriptionAsync(int userId)
        {
            return await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive && s.EndDate > DateTime.UtcNow);
        }
    }
}
