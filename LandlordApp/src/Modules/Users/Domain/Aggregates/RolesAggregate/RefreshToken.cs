namespace Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;

public class RefreshToken
{
    public int Id { get; set; }
    public string TokenHash { get; set; } = null!; // SHA-256 hex hash of the raw token
    public int UserId { get; set; }
    /// <summary>The session (one sign-in on one device): every token a refresh hands out keeps it.</summary>
    public Guid FamilyId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; } = false;
    /// <summary>When this token was exchanged for the next one; set means it must not come back.</summary>
    public DateTime? RotatedAt { get; set; }
    /// <summary>The sign-in of this session was confirmed with an authenticator code.</summary>
    public bool TwoFactorVerified { get; set; }
    public virtual User User { get; set; } = null!;
}
