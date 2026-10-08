using System.Security.Cryptography;
using System.Text;

namespace Lander.src.Modules.Payments.Paddle;

/// <summary>
/// Verifies Paddle webhook signatures.
///
/// Paddle signs each webhook with the notification-destination secret and sends the
/// signature in the <c>Paddle-Signature</c> header, formatted <c>ts=...;h1=...</c>.
/// The signed payload is the string <c>{ts}:{rawBody}</c>, HMAC-SHA256 with the secret.
///
/// CRITICAL: verification must run against the exact raw request bytes. Any JSON
/// parse/re-serialize before this check changes the bytes and breaks the signature.
/// </summary>
public sealed class PaddleSignatureVerifier
{
    /// <summary>
    /// Returns true if <paramref name="signatureHeader"/> is a valid signature for
    /// <paramref name="rawBody"/> under <paramref name="secret"/>, and (when
    /// <paramref name="maxAgeSeconds"/> &gt; 0) the timestamp is recent enough.
    /// </summary>
    public bool Verify(string? signatureHeader, string rawBody, string secret, int maxAgeSeconds)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader) || string.IsNullOrWhiteSpace(secret))
            return false;

        if (!TryParseHeader(signatureHeader, out var ts, out var h1))
            return false;

        // Replay protection: reject a captured request whose timestamp is too old.
        if (maxAgeSeconds > 0)
        {
            if (!long.TryParse(ts, out var tsUnix))
                return false;
            var age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - tsUnix;
            // Allow a small negative skew (Paddle clock slightly ahead of ours).
            if (age > maxAgeSeconds || age < -maxAgeSeconds)
                return false;
        }

        var signedPayload = $"{ts}:{rawBody}";
        var expected = ComputeHmacHex(signedPayload, secret);

        // Constant-time comparison to avoid leaking the signature via timing.
        var expectedBytes = Encoding.ASCII.GetBytes(expected);
        var actualBytes = Encoding.ASCII.GetBytes(h1);
        if (expectedBytes.Length != actualBytes.Length)
            return false;
        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    private static bool TryParseHeader(string header, out string ts, out string h1)
    {
        ts = string.Empty;
        h1 = string.Empty;
        // Format: "ts=1671552777;h1=eb4d0dc8..."
        foreach (var part in header.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var idx = part.IndexOf('=');
            if (idx <= 0) continue;
            var key = part[..idx];
            var value = part[(idx + 1)..];
            if (key == "ts") ts = value;
            else if (key == "h1") h1 = value;
        }
        return ts.Length > 0 && h1.Length > 0;
    }

    private static string ComputeHmacHex(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
