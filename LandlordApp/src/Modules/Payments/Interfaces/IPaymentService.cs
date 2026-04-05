using System.Threading.Tasks;
using Lander.src.Modules.Payments.Models;

namespace Lander.src.Modules.Payments.Interfaces
{
    public interface IPaymentService
    {
        Task<string> InitiateCheckoutAsync(int userId, string planType, decimal amount);
        Task<bool> ProcessWebhookAsync(string paytenTransactionId, string status);
        Task<Subscription?> GetActiveSubscriptionAsync(int userId);
    }
}
