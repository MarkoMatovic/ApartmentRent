using Lander.Helpers;
using Lander.src.Modules.Communication.Intefaces;
using Lander.src.Modules.Communication.Models;
using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Options;
using System.Security.Claims;
namespace Lander.src.Modules.Communication.Implementation;
public class EmailService : IEmailService
{
    private readonly SendGridClient _sendGridClient;
    private readonly SendGridSettings _settings;
    private readonly CommunicationsContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<EmailService> _logger;
    public EmailService(
        IOptions<SendGridSettings> settings, 
        CommunicationsContext context, 
        IHttpContextAccessor httpContextAccessor,
        ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _sendGridClient = new SendGridClient(_settings.ApiKey);
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }
    public async Task<bool> SendEmailAsync(string to, string subject, string htmlContent)
    {
        var from = new EmailAddress(_settings.SenderEmail, _settings.SenderName);
        var toAddress = new EmailAddress(to);
        var msg = MailHelper.CreateSingleEmail(from, toAddress, subject, null, htmlContent);
        try
        {
            var response = await _sendGridClient.SendEmailAsync(msg);
            var isSuccess = response.StatusCode == System.Net.HttpStatusCode.OK || 
                           response.StatusCode == System.Net.HttpStatusCode.Accepted;
            string? messageId = null;
            if (response.Headers.TryGetValues("X-Message-Id", out var values))
            {
                messageId = values.FirstOrDefault();
            }
            await LogEmailAsync(null, to, subject, htmlContent, null, isSuccess, messageId, null);
            return isSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            await LogEmailAsync(null, to, subject, htmlContent, null, false, null, ex.Message);
            return false;
        }
    }
    public async Task<bool> SendTemplatedEmailAsync(string to, string subject, string templateName, object templateData)
    {
        var htmlContent = RenderTemplate(templateName, templateData);
        return await SendEmailAsync(to, subject, htmlContent);
    }
    public async Task<bool> SendBulkEmailAsync(List<string> recipients, string subject, string htmlContent)
    {
        var from = new EmailAddress(_settings.SenderEmail, _settings.SenderName);
        var tos = recipients.Select(r => new EmailAddress(r)).ToList();
        var msg = MailHelper.CreateSingleEmailToMultipleRecipients(from, tos, subject, null, htmlContent);
        try
        {
            var response = await _sendGridClient.SendEmailAsync(msg);
            var isSuccess = response.StatusCode == System.Net.HttpStatusCode.OK || 
                           response.StatusCode == System.Net.HttpStatusCode.Accepted;
            string? messageId = null;
            if (response.Headers.TryGetValues("X-Message-Id", out var values))
            {
                messageId = values.FirstOrDefault();
            }
            foreach (var recipient in recipients)
            {
                await LogEmailAsync(null, recipient, subject, htmlContent, null, isSuccess, messageId, null);
            }
            return isSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send bulk email to {Count} recipients", recipients.Count);
            foreach (var recipient in recipients)
            {
                await LogEmailAsync(null, recipient, subject, htmlContent, null, false, null, ex.Message);
            }
            return false;
        }
    }
    public async Task<bool> SendWelcomeEmailAsync(string to, string userName)
    {
        var subject = "Welcome to Landlander Platform!";
        var templateData = new { UserName = userName };
        return await SendTemplatedEmailAsync(to, subject, "WelcomeEmail", templateData);
    }
    public async Task<bool> SendNewApplicationEmailAsync(string to, string landlordName, string apartmentTitle)
    {
        var subject = "New Application for Your Apartment";
        var templateData = new { LandlordName = landlordName, ApartmentTitle = apartmentTitle };
        return await SendTemplatedEmailAsync(to, subject, "NewApplicationEmail", templateData);
    }
    public async Task<bool> SendApplicationStatusEmailAsync(string to, string tenantName, string apartmentTitle, string status)
    {
        var subject = $"Application Status Update - {apartmentTitle}";
        var templateData = new { TenantName = tenantName, ApartmentTitle = apartmentTitle, Status = status };
        return await SendTemplatedEmailAsync(to, subject, "ApplicationStatusEmail", templateData);
    }
    public async Task<bool> SendNewMessageEmailAsync(string to, string senderName, string messagePreview)
    {
        var subject = $"New Message from {senderName}";
        var templateData = new { SenderName = senderName, MessagePreview = messagePreview };
        return await SendTemplatedEmailAsync(to, subject, "NewMessageEmail", templateData);
    }
    public async Task<bool> SendAppointmentConfirmationEmailAsync(string to, string userName, DateTime appointmentDate, string apartmentTitle)
    {
        var subject = "Appointment Confirmation";
        var templateData = new { UserName = userName, AppointmentDate = appointmentDate.ToString("f"), ApartmentTitle = apartmentTitle };
        return await SendTemplatedEmailAsync(to, subject, "AppointmentConfirmationEmail", templateData);
    }
    public async Task<bool> SendSavedSearchAlertEmailAsync(string to, int matchCount, string searchCriteria)
    {
        var subject = $"New Matches for Your Saved Search ({matchCount})";
        var templateData = new { MatchCount = matchCount, SearchCriteria = searchCriteria };
        return await SendTemplatedEmailAsync(to, subject, "SavedSearchAlertEmail", templateData);
    }
    public async Task<bool> SendListingUnavailableEmailAsync(string to, string userName, string apartmentTitle, string reason)
    {
        var subject = $"Update on saved listing: {apartmentTitle}";
        var templateData = new { UserName = userName, ApartmentTitle = apartmentTitle, Reason = reason };
        return await SendTemplatedEmailAsync(to, subject, "ListingUnavailableEmail", templateData);
    }
    private async Task LogEmailAsync(int? userId, string recipientEmail, string subject, string htmlContent, 
        string? templateId, bool isDelivered, string? sendGridMessageId, string? errorMessage)
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var log = new EmailLog
        {
            UserId = userId,
            RecipientEmail = recipientEmail,
            Subject = subject,
            HtmlContent = htmlContent,
            TemplateId = templateId,
            SentAt = DateTime.UtcNow,
            IsDelivered = isDelivered,
            SendGridMessageId = sendGridMessageId,
            ErrorMessage = errorMessage,
            CreatedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null,
            CreatedDate = DateTime.UtcNow
        };
        try
        {
            _context.EmailLogs.Add(log);
            await _context.SaveEntitiesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log email to database");
        }
    }
    private string RenderTemplate(string templateName, object templateData)
    {
        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "src", "Modules", "Communication", "EmailTemplates", $"{templateName}.html");
        if (!File.Exists(templatePath))
        {
            _logger.LogWarning("Template {TemplateName} not found at {Path}", templateName, templatePath);
            return $"<h1>Template {templateName} not found</h1>";
        }
        var template = File.ReadAllText(templatePath);
        foreach (var prop in templateData.GetType().GetProperties())
        {
            var placeholder = $"{{{{{prop.Name}}}}}";
            var value = prop.GetValue(templateData)?.ToString() ?? "";
            template = template.Replace(placeholder, value);
        }
        return template;
    }
}
