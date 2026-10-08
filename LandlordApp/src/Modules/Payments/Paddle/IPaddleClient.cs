namespace Lander.src.Modules.Payments.Paddle;

/// <summary>Server-to-server calls to the Paddle Billing API.</summary>
public interface IPaddleClient
{
    /// <summary>
    /// Creates a Paddle transaction for a single price and returns its id (<c>txn_...</c>).
    /// The <paramref name="customData"/> is stored server-side on the transaction and echoed
    /// back on the webhook, so it cannot be tampered with by the browser. The frontend opens
    /// the inline checkout with the returned transaction id.
    /// </summary>
    Task<string> CreateTransactionAsync(
        string priceId,
        IReadOnlyDictionary<string, string> customData,
        CancellationToken ct = default);
}
