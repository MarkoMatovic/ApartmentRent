namespace Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;

public class RefreshToken
{
    public int Id { get; set; }
    public string TokenHash { get; set; } = null!; // SHA-256 hex hash of the raw token
    public int UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; } = false;
    public virtual User User { get; set; } = null!;
}
