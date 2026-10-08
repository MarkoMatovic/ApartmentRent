namespace Lander.src.Modules.Payments.Interfaces;

public enum ReversalOutcome
{
    /// <summary>The purchase was found and its benefits were taken back.</summary>
    Reversed,
    /// <summary>This transaction was already reversed (webhook retry / duplicate event).</summary>
    AlreadyReversed,
    /// <summary>No fulfilled order exists for the transaction (never delivered, or unknown).</summary>
    OrderNotFound,
    /// <summary>
    /// The order predates purchase tracking (legacy row without user/plan) so it cannot be
    /// reversed automatically.
    /// </summary>
    NotReversible
}

/// <summary>
/// Takes back what a purchase granted when the provider fully refunds it or the buyer wins a
/// chargeback. Idempotent per transaction: the reversal is claimed atomically on the order row.
/// </summary>
public interface IPaymentReversalService
{
    /// <param name="transactionId">Provider transaction id (same value used as the order number).</param>
    /// <param name="adjustmentId">Provider adjustment id that triggered the reversal (audit trail).</param>
    /// <param name="isChargeback">True for a chargeback, false for a refund (only changes wording).</param>
    Task<ReversalOutcome> ReverseAsync(string transactionId, string adjustmentId, bool isChargeback);
}
