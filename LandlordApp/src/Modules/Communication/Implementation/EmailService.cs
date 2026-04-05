using Lander.Helpers;
using Lander.src.Modules.Communication.Interfaces;
using Lander.src.Modules.Communication.Models;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using System.Security.Claims;
using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;
using SendSmtpEmail = sib_api_v3_sdk.Model.SendSmtpEmail;
using SendSmtpEmailSender = sib_api_v3_sdk.Model.SendSmtpEmailSender;
using SendSmtpEmailTo = sib_api_v3_sdk.Model.SendSmtpEmailTo;
using SdkTask = System.Threading.Tasks.Task;

namespace Lander.src.Modules.Communication.Implementation;

public class EmailService : IEmailService
{
    private static readonly ResiliencePipeline _pipeline = new ResiliencePipelineBuilder()
        .AddRetry(new Polly.Retry.RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder().Handle<Exception>()
        })
        .AddCircuitBreaker(new Polly.CircuitBreaker.CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            MinimumThroughput = 5,
            BreakDuration = TimeSpan.FromMinutes(1),
            ShouldHandle = new PredicateBuilder().Handle<Exception>()
        })
        .Build();

    private readonly BrevoSettings _settings;
    private readonly CommunicationsContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<BrevoSettings> settings,
        CommunicationsContext context,
        IHttpContextAccessor httpContextAccessor,
        ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        Configuration.Default.AddApiKey("api-key", _settings.ApiKey);
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string htmlContent)
    {
        try
        {
            var apiInstance = new TransactionalEmailsApi();

            var sender = new SendSmtpEmailSender(_settings.SenderName, _settings.SenderEmail);
            var toList = new List<SendSmtpEmailTo> { new SendSmtpEmailTo(to) };

            var sendSmtpEmail = new SendSmtpEmail(
                sender: sender,
                to: toList,
                subject: subject,
                htmlContent: htmlContent
            );

            var result = await _pipeline.ExecuteAsync(async ct =>
                await Task.Run(() => apiInstance.SendTransacEmail(sendSmtpEmail), ct));

            var messageId = result?.MessageId;
            await LogEmailAsync(null, to, subject, htmlContent, null, true, messageId, null);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Brevo: Failed to send email to {To}", to);
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
        try
        {
            var apiInstance = new TransactionalEmailsApi();

            var sender = new SendSmtpEmailSender(_settings.SenderName, _settings.SenderEmail);
            var toList = recipients.Select(r => new SendSmtpEmailTo(r)).ToList();

            var sendSmtpEmail = new SendSmtpEmail(
                sender: sender,
                to: toList,
                subject: subject,
                htmlContent: htmlContent
            );

            var result = await Task.Run(() => apiInstance.SendTransacEmail(sendSmtpEmail));
            var messageId = result?.MessageId;

            foreach (var recipient in recipients)
            {
                await LogEmailAsync(null, recipient, subject, htmlContent, null, true, messageId, null);
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Brevo: Failed to send bulk email to {Count} recipients", recipients.Count);
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
    public async Task<bool> SendEmailVerificationAsync(string to, string userName, string verificationLink)
    {
        var subject = "Verifikacija email adrese";
        var templateData = new { UserName = userName, VerificationLink = verificationLink };
        return await SendTemplatedEmailAsync(to, subject, "EmailVerificationEmail", templateData);
    }

    public async Task<bool> SendPasswordResetEmailAsync(string to, string userName, string resetLink)
    {
        var subject = "Reset lozinke";
        var templateData = new { UserName = userName, ResetLink = resetLink };
        return await SendTemplatedEmailAsync(to, subject, "PasswordResetEmail", templateData);
    }

    private async Task LogEmailAsync(int? userId, string recipientEmail, string subject, string htmlContent,
        string? templateId, bool isDelivered, string? providerMessageId, string? errorMessage)
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
            ProviderMessageId = providerMessageId,
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

