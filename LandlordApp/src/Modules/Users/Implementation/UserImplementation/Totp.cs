using System.Security.Cryptography;
using System.Text;

namespace Lander.src.Modules.Users.Implementation.UserImplementation;

/// <summary>
/// Time-based one-time passwords (RFC 6238) as authenticator apps (Google, Microsoft, Authy...) make
/// them: HMAC-SHA1, 30-second steps, 6 digits. Secrets travel as Base32 (RFC 4648), the format the
/// apps read from the QR code or accept typed in.
/// </summary>
public static class Totp
{
    public const int Digits = 6;
    public static readonly TimeSpan Step = TimeSpan.FromSeconds(30);

    /// <summary>160 random bits, the size RFC 4226 recommends for HMAC-SHA1.</summary>
    public static byte[] NewSecret() => RandomNumberGenerator.GetBytes(20);

    public static long StepAt(DateTimeOffset time) => time.ToUnixTimeSeconds() / (long)Step.TotalSeconds;

    /// <summary>The code for one time step (RFC 4226 dynamic truncation), zero-padded.</summary>
    public static string Compute(byte[] secret, long step, int digits = Digits)
    {
        Span<byte> counter = stackalloc byte[8];
        for (var i = 7; i >= 0; i--)
        {
            counter[i] = (byte)(step & 0xff);
            step >>= 8;
        }

        Span<byte> hash = stackalloc byte[20];
        HMACSHA1.HashData(secret, counter, hash);
        var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24)
                     | (hash[offset + 1] << 16)
                     | (hash[offset + 2] << 8)
                     | hash[offset + 3];
        var modulo = (int)Math.Pow(10, digits);
        return (binary % modulo).ToString(new string('0', digits));
    }

    /// <summary>
    /// The time step the code belongs to, or null. One step either side is accepted, because the
    /// phone's clock and the server's are rarely exactly the same and typing takes a moment.
    /// </summary>
    public static long? Match(byte[] secret, string code, DateTimeOffset now, int window = 1)
    {
        var normalized = new string(code.Where(char.IsDigit).ToArray());
        if (normalized.Length != Digits) return null;

        var current = StepAt(now);
        long? match = null;
        // Every candidate is checked (no early exit), so the time taken says nothing about the code.
        for (var step = current - window; step <= current + window; step++)
        {
            var expected = Encoding.ASCII.GetBytes(Compute(secret, step));
            if (CryptographicOperations.FixedTimeEquals(expected, Encoding.ASCII.GetBytes(normalized)))
                match ??= step;
        }
        return match;
    }

    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string ToBase32(byte[] data)
    {
        var output = new StringBuilder((data.Length * 8 + 4) / 5);
        int buffer = 0, bits = 0;
        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                output.Append(Base32Alphabet[(buffer >> (bits - 5)) & 31]);
                bits -= 5;
            }
        }
        if (bits > 0) output.Append(Base32Alphabet[(buffer << (5 - bits)) & 31]);
        return output.ToString();
    }

    public static byte[] FromBase32(string text)
    {
        var clean = text.Trim().TrimEnd('=').Replace(" ", "").ToUpperInvariant();
        var output = new List<byte>(clean.Length * 5 / 8);
        int buffer = 0, bits = 0;
        foreach (var c in clean)
        {
            var value = Base32Alphabet.IndexOf(c);
            if (value < 0) throw new FormatException($"'{c}' is not a Base32 character.");
            buffer = (buffer << 5) | value;
            bits += 5;
            if (bits >= 8)
            {
                output.Add((byte)((buffer >> (bits - 8)) & 0xff));
                bits -= 8;
            }
        }
        return output.ToArray();
    }

    /// <summary>The link the QR code carries; authenticator apps read the account name and issuer from it.</summary>
    public static string SetupUri(string issuer, string account, byte[] secret) =>
        $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(account)}" +
        $"?secret={ToBase32(secret)}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits={Digits}&period={(int)Step.TotalSeconds}";
}
