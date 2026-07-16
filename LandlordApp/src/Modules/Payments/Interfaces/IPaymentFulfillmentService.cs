namespace Lander.src.Modules.Payments.Interfaces;

/// <summary>
/// Provider-agnostic fulfillment of a paid order: grants tokens, listing credits,
/// featured listings, profile boosts, priority inbox, or analytics based on the plan.
/// A payment provider (the one we plug in next) calls <see cref="FulfillAsync"/> once it
/// has confirmed a charge. Fulfillment is idempotent — guarded by a unique
/// <c>orderReference</c> — so duplicate provider callbacks never grant twice.
/// </summary>
public interface IPaymentFulfillmentService
{
    /// <summary>
    /// Idempotently fulfills a confirmed payment.
    /// </summary>
    /// <param name="orderReference">Unique provider order/transaction reference (idempotency key).</param>
    /// <param name="userId">The buyer.</param>
    /// <param name="planId">Plan identifier (see <see cref="Lander.Helpers.PlanConstants"/>).</param>
    /// <param name="apartmentId">Required for featured-* plans; ignored otherwise.</param>
    /// <returns>True if the order is fulfilled (now or previously); false if it was a no-op.</returns>
    Task<bool> FulfillAsync(string orderReference, int userId, string planId, int? apartmentId = null);
}
