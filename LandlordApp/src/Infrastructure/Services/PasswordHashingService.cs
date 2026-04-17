namespace Lander.src.Infrastructure.Services;

public class PasswordHashingService : IPasswordHashingService
{
    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string hash)
    {
        if (hash.StartsWith("$2"))
            return BCrypt.Net.BCrypt.Verify(password, hash);

        // Legacy SHA-256 fallback (gradual migration path)
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var legacyHash = Convert.ToBase64String(
            sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password)));
        return legacyHash == hash;
    }

    public bool NeedsRehash(string hash) => !hash.StartsWith("$2");
}
