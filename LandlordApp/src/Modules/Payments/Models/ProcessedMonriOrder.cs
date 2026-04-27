namespace Lander.src.Modules.Payments.Models;

public class ProcessedMonriOrder
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = null!;
    public DateTimeOffset ProcessedAt { get; set; }
}
