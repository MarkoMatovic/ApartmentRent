namespace Lander.src.Modules.Payments.Dtos;

public class PaytenWebhookPayloadDto
{
    public string TransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
