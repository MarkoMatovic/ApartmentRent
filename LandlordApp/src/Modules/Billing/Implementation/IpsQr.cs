using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using QRCoder;

namespace Lander.src.Modules.Billing.Implementation;

/// <summary>
/// Serbian bank account numbers ("tekući račun"): three digits of the bank, up to thirteen of the
/// account and two control digits, written as 160-0000000123456-78. The whole 18-digit number is
/// valid when it leaves 1 divided by 97 (ISO 7064 MOD 97-10), which catches mistyped digits.
/// </summary>
public static class SerbianAccount
{
    private static readonly Regex Grouped = new(@"^(\d{3})-(\d{1,13})-(\d{2})$", RegexOptions.Compiled);
    private static readonly Regex Plain = new(@"^\d{18}$", RegexOptions.Compiled);

    /// <summary>The 18 digits of a valid account, from "160-123456-78", "160 123456 78" or 18 digits; null when it is not one.</summary>
    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var text = input.Trim().Replace(' ', '-');
        while (text.Contains("--")) text = text.Replace("--", "-");

        string digits;
        var grouped = Grouped.Match(text);
        if (grouped.Success)
            digits = grouped.Groups[1].Value + grouped.Groups[2].Value.PadLeft(13, '0') + grouped.Groups[3].Value;
        else if (Plain.IsMatch(text))
            digits = text;
        else
            return null;

        return Mod97(digits) == 1 ? digits : null;
    }

    public static bool IsValid(string? input) => Normalize(input) is not null;

    /// <summary>"160000000012345678" → "160-0000000123456-78".</summary>
    public static string Format(string digits) =>
        digits.Length == 18 ? $"{digits[..3]}-{digits[3..16]}-{digits[16..]}" : digits;

    /// <summary>The two control digits that make the 16 given digits a valid account (for tests and examples).</summary>
    public static string ControlDigits(string sixteenDigits) =>
        (98 - Mod97(sixteenDigits + "00")).ToString("00", CultureInfo.InvariantCulture);

    private static int Mod97(string digits)
    {
        var remainder = 0;
        foreach (var c in digits) remainder = (remainder * 10 + (c - '0')) % 97;
        return remainder;
    }
}

/// <summary>What a payment order needs; the QR code carries exactly this.</summary>
public sealed record IpsPayment(
    string PayeeAccount,
    string PayeeName,
    decimal Amount,
    string PaymentCode,
    string Purpose,
    string? PaymentModel,
    string? PaymentReference,
    string? PayerName);

/// <summary>
/// The NBS IPS QR code ("IPS skeniraj"), as printed on utility bills: a banking app reads it and fills
/// in the whole payment order. Only dinars; the payment arrives within seconds through the instant
/// payment system. Tags in the order of the NBS standard: K, V, C, R, N, I, P, SF, S, RO.
/// </summary>
public static class IpsQr
{
    public const int MaxNameLength = 70;
    public const int MaxPurposeLength = 35;
    public const int MaxReferenceLength = 22;

    public static string Payload(IpsPayment payment)
    {
        if (payment.PayeeAccount.Length != 18 || !payment.PayeeAccount.All(char.IsDigit))
            throw new ArgumentException("The account must be 18 digits.", nameof(payment));
        if (payment.Amount <= 0)
            throw new ArgumentException("The amount must be positive.", nameof(payment));

        var builder = new StringBuilder();
        builder.Append("K:PR|V:01|C:1");
        builder.Append("|R:").Append(payment.PayeeAccount);
        builder.Append("|N:").Append(Clean(payment.PayeeName, MaxNameLength));
        builder.Append("|I:RSD").Append(Amount(payment.Amount));
        if (!string.IsNullOrWhiteSpace(payment.PayerName))
            builder.Append("|P:").Append(Clean(payment.PayerName, MaxNameLength));
        builder.Append("|SF:").Append(payment.PaymentCode);
        builder.Append("|S:").Append(Clean(payment.Purpose, MaxPurposeLength));
        if (!string.IsNullOrWhiteSpace(payment.PaymentReference))
            builder.Append("|RO:").Append(payment.PaymentModel ?? "00").Append(payment.PaymentReference);
        return builder.ToString();
    }

    /// <summary>The amount as the standard writes it: a comma before the para, no thousands separator ("3596,13").</summary>
    public static string Amount(decimal amount) =>
        decimal.Round(amount, 2, MidpointRounding.AwayFromZero)
            .ToString("0.00", CultureInfo.InvariantCulture)
            .Replace('.', ',');

    public static string QrCodeDataUrl(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M, forceUtf8: true);
        var png = new PngByteQRCode(data).GetGraphic(8);
        return "data:image/png;base64," + Convert.ToBase64String(png);
    }

    /// <summary>One line, no "|" (it separates the tags), at most <paramref name="max"/> characters.</summary>
    private static string Clean(string value, int max)
    {
        var text = Regex.Replace(value.Replace('|', ' '), @"\s+", " ").Trim();
        return text.Length <= max ? text : text[..max].TrimEnd();
    }
}
