namespace Lander.src.Modules.Payments.Dtos;

public class CreateCheckoutDto
{
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
}
