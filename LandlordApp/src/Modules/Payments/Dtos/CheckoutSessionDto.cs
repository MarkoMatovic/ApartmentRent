namespace Lander.src.Modules.Payments.Dtos;

public class CheckoutSessionDto
{
    public string SessionId { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
}
