namespace Lander.src.Modules.Communication.Models;

/// <summary>
/// Outbox pattern: events written atomically with the message, processed by OutboxProcessorService.
/// Guarantees at-least-once delivery across DbContext boundaries (CommunicationsContext → UsersContext).
/// </summary>
public class OutboxMessage
{
    public int Id { get; set; }
    public string EventType { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }
}
