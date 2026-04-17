namespace Lander.src.Infrastructure.Services;

public interface IPasswordHashingService
{
    string Hash(string password);
    bool Verify(string password, string hash);
    bool NeedsRehash(string hash);
}
