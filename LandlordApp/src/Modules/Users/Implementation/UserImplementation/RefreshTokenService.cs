using System.Security.Cryptography;
using System.Text;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Users.Implementation.UserImplementation;

public class RefreshTokenService
{
    private readonly UsersContext _context;

    public RefreshTokenService(UsersContext context)
    {
        _context = context;
    }

    public string GenerateRawToken() =>
        Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

    public static string HashToken(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLower();
    }

    public async Task<string> CreateAsync(int userId)
    {
        // Revokuj sve postojece aktivne tokene za ovog usera (jedan aktivni session)
        var existing = await _context.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
        foreach (var t in existing) t.IsRevoked = true;

        var raw = GenerateRawToken();
        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            TokenHash = HashToken(raw),
            ExpiresAt = DateTime.UtcNow.AddDays(30),
        });

        await _context.SaveEntitiesAsync();
        return raw;
    }

    public async Task<RefreshToken?> ValidateAsync(string raw)
    {
        var hash = HashToken(raw);
        return await _context.RefreshTokens
            .Include(t => t.User).ThenInclude(u => u.UserRole)
            .FirstOrDefaultAsync(t =>
                t.TokenHash == hash &&
                !t.IsRevoked &&
                t.ExpiresAt > DateTime.UtcNow);
    }

    public async Task RevokeAsync(string raw)
    {
        var hash = HashToken(raw);
        var token = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
        if (token is null) return;
        token.IsRevoked = true;
        await _context.SaveEntitiesAsync();
    }
}
