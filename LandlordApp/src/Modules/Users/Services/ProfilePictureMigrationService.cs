using Lander.src.Common.Exceptions;
using Lander.src.Modules.Users.Implementation.UserImplementation;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Users.Services;

/// <summary>
/// Hangfire recurring job (hourly) that moves profile pictures still stored inline in the Users table
/// ("data:image/...;base64,...", up to ~7 MB each) to file storage and keeps only their address.
///
/// Users are converted one at a time so a run never holds more than one picture in memory. A picture
/// that is not a readable image is dropped (the user simply has no picture). If storage is unreachable
/// the run stops and the next one continues; once everything is converted a run costs one query.
///
/// Config: "ProfilePictures:MigrationBatchSize" (default 200 users per run).
/// </summary>
public sealed class ProfilePictureMigrationService
{
    private readonly UsersContext _db;
    private readonly IProfilePictureService _pictures;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProfilePictureMigrationService> _logger;

    public ProfilePictureMigrationService(
        UsersContext db,
        IProfilePictureService pictures,
        IConfiguration configuration,
        ILogger<ProfilePictureMigrationService> logger)
    {
        _db = db;
        _pictures = pictures;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<int> RunAsync()
    {
        var batchSize = Math.Max(1, _configuration.GetValue("ProfilePictures:MigrationBatchSize", 200));

        var pending = await _db.Users
            .Where(u => u.ProfilePicture != null && u.ProfilePicture.StartsWith("data:"))
            .OrderBy(u => u.UserId)
            .Select(u => u.UserId)
            .Take(batchSize)
            .ToListAsync();

        int moved = 0, dropped = 0;
        foreach (var userId in pending)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user is null || !_pictures.IsInline(user.ProfilePicture)) continue;

            string? url;
            try
            {
                url = await _pictures.StoreInlineAsync(user.UserGuid, user.ProfilePicture!);
                moved++;
            }
            catch (BusinessRuleException)
            {
                _logger.LogWarning("Profile picture of user {UserId} is not a readable image; it was removed", userId);
                url = null;
                dropped++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Profile picture migration stopped at user {UserId}; the next run continues", userId);
                break;
            }

            user.ProfilePicture = url;
            await _db.SaveChangesAsync();
            // Release the multi-megabyte string before the next user is loaded.
            _db.Entry(user).State = EntityState.Detached;
        }

        if (pending.Count > 0)
            _logger.LogInformation(
                "Profile picture migration: {Moved} moved to storage, {Dropped} unreadable removed, of {Checked} checked",
                moved, dropped, pending.Count);
        return moved;
    }
}
