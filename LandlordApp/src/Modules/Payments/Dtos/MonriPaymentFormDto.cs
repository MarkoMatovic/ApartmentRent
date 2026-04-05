namespace Lander.src.Modules.Payments.Dtos;

/// <summary>
/// Contains all fields the frontend needs to submit a payment form to Monri's hosted payment page.
/// </summary>
public class MonriPaymentFormDto
{
    public string FormAction { get; set; } = string.Empty;
    public string AuthenticityToken { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string OrderInfo { get; set; } = string.Empty;
    public string Digest { get; set; } = string.Empty;
    public string SuccessUrl { get; set; } = string.Empty;
    public string FailureUrl { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
    public string BuyerEmail { get; set; } = string.Empty;
    public string BuyerName { get; set; } = string.Empty;
}
