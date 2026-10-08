namespace Lander.src.Modules.Payments.Models;

/// <summary>
/// One row per fulfilled payment. Doubles as the idempotency guard (unique OrderNumber)
/// and as the record needed to reverse the purchase if Paddle later refunds it.
/// </summary>
public class ProcessedOrder
{
    public int Id { get; set; }

    /// <summary>Provider transaction id (Paddle <c>txn_…</c>) — the idempotency key.</summary>
    public string OrderNumber { get; set; } = null!;

    public DateTimeOffset ProcessedAt { get; set; }

    // ── Purchase details (null on legacy Monri-era rows) ────────────────────
    public int? UserId { get; set; }
    public string? PlanId { get; set; }
    public int? ApartmentId { get; set; }

    // ── Reversal (refund / chargeback) ──────────────────────────────────────
    /// <summary>Set once, atomically, when a full refund/chargeback has been applied.</summary>
    public DateTimeOffset? ReversedAt { get; set; }

    /// <summary>Paddle adjustment id (<c>adj_…</c>) that caused the reversal.</summary>
    public string? ReversalReference { get; set; }
}
