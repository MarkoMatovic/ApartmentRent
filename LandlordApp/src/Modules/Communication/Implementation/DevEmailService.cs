using Lander.src.Modules.Communication.Interfaces;

namespace Lander.src.Modules.Communication.Implementation;

/// <summary>
/// No-op email service for Development and E2eTesting environments.
/// Logs every outgoing email and writes the HTML body to a temp file so
/// developers can inspect rendering without sending real messages.
/// </summary>
public sealed class DevEmailService : IEmailService
{
    private readonly ILogger<DevEmailService> _logger;
    private readonly string _outputDir;

    public DevEmailService(ILogger<DevEmailService> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _outputDir = Path.Combine(Path.GetTempPath(), "landlander-emails");
        Directory.CreateDirectory(_outputDir);
    }

    public Task<bool> SendEmailAsync(string to, string subject, string htmlContent)
        => LogAndSave(to, subject, htmlContent);

    public Task<bool> SendTemplatedEmailAsync(string to, string subject, string templateName, object templateData)
    {
        _logger.LogInformation("[DEV-EMAIL] Template={Template} To={To} Subject={Subject} Data={@Data}",
            templateName, to, subject, templateData);
        return Task.FromResult(true);
    }

    public Task<bool> SendBulkEmailAsync(List<string> recipients, string subject, string htmlContent)
    {
        _logger.LogInformation("[DEV-EMAIL] Bulk To={Count} recipients Subject={Subject}", recipients.Count, subject);
        return Task.FromResult(true);
    }

    public Task<bool> SendWelcomeEmailAsync(string to, string userName)
        => LogAndSave(to, $"Dobrodošli, {userName}!", $"<p>Dobrodošli na platformu, <strong>{userName}</strong>!</p>");

    public Task<bool> SendNewApplicationEmailAsync(string to, string landlordName, string apartmentTitle)
        => LogAndSave(to, "Nova prijava za oglas", $"<p>Novi zakupac se prijavio za <strong>{apartmentTitle}</strong>.</p>");

    public Task<bool> SendApplicationStatusEmailAsync(string to, string tenantName, string apartmentTitle, string status)
        => LogAndSave(to, $"Status prijave: {status}", $"<p>Vaša prijava za <strong>{apartmentTitle}</strong> je: <strong>{status}</strong>.</p>");

    public Task<bool> SendNewMessageEmailAsync(string to, string senderName, string messagePreview)
        => LogAndSave(to, $"Nova poruka od {senderName}", $"<p>{senderName}: <em>{messagePreview}</em></p>");

    public Task<bool> SendAppointmentConfirmationEmailAsync(string to, string userName, DateTime appointmentDate, string apartmentTitle)
        => LogAndSave(to, "Potvrda termina", $"<p>Termin za <strong>{apartmentTitle}</strong> u {appointmentDate:dd.MM.yyyy HH:mm} je potvrđen.</p>");

    public Task<bool> SendSavedSearchAlertEmailAsync(string to, int matchCount, string searchCriteria)
        => LogAndSave(to, $"{matchCount} novih oglasa", $"<p>Pronađeno <strong>{matchCount}</strong> oglasa koji odgovaraju: {searchCriteria}.</p>");

    public Task<bool> SendListingUnavailableEmailAsync(string to, string userName, string apartmentTitle, string reason)
        => LogAndSave(to, $"Oglas nedostupan: {apartmentTitle}", $"<p>Oglas <strong>{apartmentTitle}</strong> je uklonjen. Razlog: {reason}.</p>");

    public Task<bool> SendEmailVerificationAsync(string to, string userName, string verificationLink)
        => LogAndSave(to, "Verifikacija emaila", $"<p>Klikni <a href=\"{verificationLink}\">ovdje</a> da potvrdis email adresu.</p>");

    public Task<bool> SendPasswordResetEmailAsync(string to, string userName, string resetLink)
        => LogAndSave(to, "Resetovanje lozinke", $"<p>Klikni <a href=\"{resetLink}\">ovdje</a> da resetujes lozinku.</p>");

    public Task<bool> SendOrderConfirmationEmailAsync(string to, string userName, string planName, string orderNumber, decimal amountEur)
        => LogAndSave(to, $"Potvrda narudžbe — {planName}",
            $"<p>Dragi {userName},</p><p>Vaša narudžba <strong>{orderNumber}</strong> za <strong>{planName}</strong> " +
            $"u iznosu od <strong>{amountEur:F2} EUR</strong> je uspješno obrađena.</p>");

    private Task<bool> LogAndSave(string to, string subject, string htmlContent)
    {
        _logger.LogInformation("[DEV-EMAIL] To={To} Subject={Subject}", to, subject);

        var fileName = $"{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}_{Path.GetRandomFileName()}.html";
        var filePath = Path.Combine(_outputDir, fileName);

        var html = $$"""
            <!DOCTYPE html><html><head>
            <meta charset="utf-8">
            <title>{{subject}}</title>
            <style>body{font-family:sans-serif;padding:24px;}
            .meta{background:#f0f0f0;padding:12px;border-radius:4px;margin-bottom:16px;font-size:12px;}
            </style></head><body>
            <div class="meta">
              <strong>To:</strong> {{to}}<br>
              <strong>Subject:</strong> {{subject}}<br>
              <strong>Sent:</strong> {{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}} UTC
            </div>
            {{htmlContent}}
            </body></html>
            """;

        // Best-effort — never block or fail the caller
        _ = File.WriteAllTextAsync(filePath, html);
        _logger.LogDebug("[DEV-EMAIL] HTML saved → {Path}", filePath);

        return Task.FromResult(true);
    }
}
