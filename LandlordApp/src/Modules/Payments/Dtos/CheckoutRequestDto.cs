namespace Lander.src.Modules.Payments.Dtos;

public class CheckoutRequestDto
{
    public string PlanType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
