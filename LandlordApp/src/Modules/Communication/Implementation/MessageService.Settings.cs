using System.Security.Claims;
using Lander.src.Common;
using Lander.src.Modules.Communication.Dtos.InputDto;
using Lander.src.Modules.Communication.Models;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Communication.Implementation;
public partial class MessageService
{
    private async Task<ConversationSettings> GetOrCreateSettingsAsync(int userId, int otherUserId)
    {
        var settings = await _context.ConversationSettings
            .FirstOrDefaultAsync(s => s.UserId == userId && s.OtherUserId == otherUserId);

        if (settings == null)
        {
            var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var candidate = new ConversationSettings
            {
                UserId = userId,
                OtherUserId = otherUserId,
                IsArchived = false,
                IsMuted = false,
                IsBlocked = false,
                CreatedByGuid = Guid.TryParse(currentUserGuid, out var settingsGuid) ? settingsGuid : null,
                CreatedDate = DateTime.UtcNow
            };
            _context.ConversationSettings.Add(candidate);
            try
            {
                await _context.SaveEntitiesAsync();
                settings = candidate;
            }
            catch (DbUpdateException)
            {
                // Another concurrent request inserted the row first — fetch the winner
                _context.ConversationSettings.Remove(candidate);
                settings = await _context.ConversationSettings
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.OtherUserId == otherUserId)
                    ?? throw new InvalidOperationException(
                        $"ConversationSettings for ({userId},{otherUserId}) missing after conflict.");
            }
        }

        return settings;
    }

    private async Task UpdateConversationSettingAsync(int userId, int otherUserId, Action<ConversationSettings> apply)
    {
        var settings = await GetOrCreateSettingsAsync(userId, otherUserId);
        apply(settings);
        settings.ModifiedDate = DateTime.UtcNow;
        await _context.SaveEntitiesAsync();
    }

    public Task ArchiveConversationAsync(int userId, int otherUserId)
        => UpdateConversationSettingAsync(userId, otherUserId, s => s.IsArchived = true);

    public Task UnarchiveConversationAsync(int userId, int otherUserId)
        => UpdateConversationSettingAsync(userId, otherUserId, s => s.IsArchived = false);

    public Task MuteConversationAsync(int userId, int otherUserId)
        => UpdateConversationSettingAsync(userId, otherUserId, s => s.IsMuted = true);

    public Task UnmuteConversationAsync(int userId, int otherUserId)
        => UpdateConversationSettingAsync(userId, otherUserId, s => s.IsMuted = false);

    public Task BlockUserAsync(int userId, int blockedUserId)
        => UpdateConversationSettingAsync(userId, blockedUserId, s => s.IsBlocked = true);

    public Task UnblockUserAsync(int userId, int blockedUserId)
        => UpdateConversationSettingAsync(userId, blockedUserId, s => s.IsBlocked = false);

    public async Task<bool> IsUserBlockedAsync(int userId, int otherUserId)
    {
        var settings = await _context.ConversationSettings
            .FirstOrDefaultAsync(s => s.UserId == userId && s.OtherUserId == otherUserId);
        return settings?.IsBlocked ?? false;
    }

    public async Task<string> UploadFileAsync(IFormFile file, int userId)
    {
        // Validate file
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty");

        if (file.Length > 3 * 1024 * 1024) // 3MB
            throw new ArgumentException("File size exceeds 3MB limit");

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".xls", ".xlsx" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            throw new ArgumentException("File type not allowed");

        // Magic byte validation for image types to prevent extension spoofing
        using (var peek = file.OpenReadStream())
        {
            var header = new byte[8];
            var read = await peek.ReadAsync(header, 0, header.Length);
            var isJpeg = read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
            var isPng = read >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
            var isGif = read >= 4 && header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38;
            var isPdf = read >= 4 && header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46;
            var isOffice = read >= 8 && header[0] == 0xD0 && header[1] == 0xCF; // legacy .doc/.xls
            var isOfficeXml = read >= 4 && header[0] == 0x50 && header[1] == 0x4B; // .docx/.xlsx (ZIP)

            var validMagic = (extension is ".jpg" or ".jpeg" && isJpeg)
                || (extension == ".png" && isPng)
                || (extension == ".gif" && isGif)
                || (extension == ".pdf" && isPdf)
                || (extension is ".doc" or ".xls" && (isOffice || isOfficeXml))
                || (extension is ".docx" or ".xlsx" && isOfficeXml);

            if (!validMagic)
                throw new ArgumentException("File content does not match its extension");
        }

        // Create upload directory if it doesn't exist
        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "chat-files");
        Directory.CreateDirectory(uploadsFolder);

        // Generate unique filename
        var uniqueFileName = $"{userId}_{DateTime.UtcNow.Ticks}{extension}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Return relative URL
        return $"/uploads/chat-files/{uniqueFileName}";
    }

    public async Task ReportAbuseAsync(int reportedByUserId, ReportMessageDto reportDto)
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        var report = new ReportedMessage
        {
            MessageId = reportDto.MessageId,
            ReportedByUserId = reportedByUserId,
            ReportedUserId = reportDto.ReportedUserId,
            Reason = reportDto.Reason,
            Status = ApplicationStatuses.Pending,
            CreatedByGuid = Guid.TryParse(currentUserGuid, out var reporterGuid) ? reporterGuid : null,
            CreatedDate = DateTime.UtcNow
        };

        var transaction = await _context.BeginTransactionAsync();
        try
        {
            _context.ReportedMessages.Add(report);
            await _context.SaveEntitiesAsync();
            await _context.CommitTransactionAsync(transaction);
        }
        catch
        {
            _context.RollBackTransaction();
            throw;
        }
    }
}
