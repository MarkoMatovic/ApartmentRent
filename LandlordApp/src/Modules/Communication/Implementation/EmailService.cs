using Lander.Helpers;
using Lander.src.Infrastructure.Services;
using Lander.src.Modules.Communication.Interfaces;
using Lander.src.Modules.Communication.Services;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Lander.src.Modules.Communication.Implementation;

/// <summary>
/// Builds the e-mails and queues them; <see cref="EmailDeliveryJob"/> sends them through Brevo in the
/// background, so the request that caused an e-mail never waits for Brevo (or for its retries).
/// </summary>
public class EmailService : IEmailService
{
    private readonly BrevoSettings _settings;
    private readonly IBackgroundScheduler _scheduler;
    private readonly CommunicationsContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<BrevoSettings> settings,
        CommunicationsContext context,
        IHttpContextAccessor httpContextAccessor,
        IEmailTemplateRenderer templateRenderer,
        ILogger<EmailService> logger,
        IBackgroundScheduler scheduler)
    {
        _settings = settings.Value;
        _scheduler = scheduler;
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _templateRenderer = templateRenderer;
        _logger = logger;
        // Guard: warn early if key is missing rather than getting cryptic 401s later.
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            logger.LogWarning("Brevo API key is not configured — email sending will fail.");
    }

    /// <summary>
    /// Without an API key nothing can be sent: the mail is logged as not delivered at once instead of
    /// queuing a job that Brevo would refuse. Local runs and tests have no key.
    /// </summary>
    private bool NotConfigured => string.IsNullOrWhiteSpace(_settings.ApiKey);
    private const string NotConfiguredError = "Brevo API key is not configured";

    /// <summary>True when the e-mail is queued (Brevo is called later, by the job).</summary>
    public Task<bool> SendEmailAsync(string to, string subject, string htmlContent) =>
        QueueAsync(new List<string> { to }, subject, htmlContent);

    /// <summary>One e-mail to all recipients, queued like a single one.</summary>
    public Task<bool> SendBulkEmailAsync(List<string> recipients, string subject, string htmlContent) =>
        QueueAsync(recipients, subject, htmlContent);

    private async Task<bool> QueueAsync(List<string> recipients, string subject, string htmlContent)
    {
        var requestedBy = Guid.TryParse(
            _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var guid) ? guid : (Guid?)null;
        if (NotConfigured)
        {
            await EmailLogWriter.WriteAsync(_context, _logger, recipients, subject, requestedBy, false, null, NotConfiguredError);
            return false;
        }
        try
        {
            var email = new QueuedEmail { To = recipients, Subject = subject, HtmlContent = htmlContent, RequestedBy = requestedBy };
            _scheduler.Enqueue<EmailDeliveryJob>(job => job.SendAsync(email, null));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not queue the email to {To}", string.Join(", ", recipients));
            await EmailLogWriter.WriteAsync(_context, _logger, recipients, subject, requestedBy, false, null, ex.Message);
            return false;
        }
    }

    public async Task<bool> SendTemplatedEmailAsync(string to, string subject, string templateName, object templateData)
    {
        var htmlContent = _templateRenderer.Render(templateName, templateData);
        return await SendEmailAsync(to, subject, htmlContent);
    }

    public async Task<bool> SendWelcomeEmailAsync(string to, string userName)
    {
        var subject = "Dobrodošli na TuRentaj!";
        var templateData = new { UserName = userName };
        return await SendTemplatedEmailAsync(to, subject, "WelcomeEmail", templateData);
    }

    public async Task<bool> SendNewApplicationEmailAsync(string to, string landlordName, string apartmentTitle)
    {
        var subject = "Nova prijava za vaš stan";
        var templateData = new { LandlordName = landlordName, ApartmentTitle = apartmentTitle };
        return await SendTemplatedEmailAsync(to, subject, "NewApplicationEmail", templateData);
    }

    public async Task<bool> SendApplicationStatusEmailAsync(string to, string tenantName, string apartmentTitle, string status)
    {
        var statusLabel = status switch
        {
            "Approved"  => "Prijava odobrena",
            "Rejected"  => "Prijava odbijena",
            "Pending"   => "Prijava na čekanju",
            "Cancelled" => "Prijava otkazana",
            _           => status
        };
        var subject = $"Status prijave: {apartmentTitle}";
        var templateData = new
        {
            TenantName     = tenantName,
            ApartmentTitle = apartmentTitle,
            Status         = status,
            StatusLower    = status.ToLowerInvariant(),   // used for CSS class
            StatusLabel    = statusLabel                   // localised display text
        };
        return await SendTemplatedEmailAsync(to, subject, "ApplicationStatusEmail", templateData);
    }

    public async Task<bool> SendNewMessageEmailAsync(string to, string senderName, string messagePreview)
    {
        var subject = $"Nova poruka od {senderName}";
        var templateData = new { SenderName = senderName, MessagePreview = messagePreview };
        return await SendTemplatedEmailAsync(to, subject, "NewMessageEmail", templateData);
    }

    public async Task<bool> SendAppointmentConfirmationEmailAsync(string to, string userName, DateTime appointmentDate, string apartmentTitle)
    {
        var subject = "Potvrda razgledanja stana";
        var templateData = new { UserName = userName, AppointmentDate = appointmentDate.ToString("dd.MM.yyyy. 'u' HH:mm", System.Globalization.CultureInfo.InvariantCulture), ApartmentTitle = apartmentTitle };
        return await SendTemplatedEmailAsync(to, subject, "AppointmentConfirmationEmail", templateData);
    }

    public async Task<bool> SendSavedSearchAlertEmailAsync(string to, int matchCount, string searchCriteria)
    {
        var subject = $"Novi stanovi za vašu sačuvanu pretragu ({matchCount})";
        var templateData = new { MatchCount = matchCount, SearchCriteria = searchCriteria };
        return await SendTemplatedEmailAsync(to, subject, "SavedSearchAlertEmail", templateData);
    }

    public async Task<bool> SendListingUnavailableEmailAsync(string to, string userName, string apartmentTitle, string reason)
    {
        var subject = $"Oglas više nije dostupan: {apartmentTitle}";
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

    public async Task<bool> SendRoommateProposalEmailAsync(string to, string userName, string proposerName)
    {
        var subject = $"{proposerName} vam predlaže da budete cimeri";
        var templateData = new { UserName = userName, ProposerName = proposerName };
        return await SendTemplatedEmailAsync(to, subject, "RoommateProposalEmail", templateData);
    }

    public async Task<bool> SendRoommateAcceptedEmailAsync(string to, string userName, string partnerName)
    {
        var subject = "Cimerstvo je potvrđeno";
        var templateData = new { UserName = userName, PartnerName = partnerName };
        return await SendTemplatedEmailAsync(to, subject, "RoommateAcceptedEmail", templateData);
    }

    public async Task<bool> SendBillReminderEmailAsync(string to, string userName, string billTitle, decimal amount, DateOnly dueDate, bool overdue)
    {
        var serbian = System.Globalization.CultureInfo.GetCultureInfo("sr-Latn-RS");
        var due = dueDate.ToString("d. M. yyyy.", serbian);
        var subject = overdue ? $"Rok je prošao: {billTitle}" : $"Podsetnik: {billTitle} dospeva {due}";
        var templateData = new
        {
            UserName = userName,
            Heading = overdue ? "Rok za plaćanje je prošao" : "Rok za plaćanje se bliži",
            BillTitle = billTitle,
            Amount = amount.ToString("N2", serbian) + " RSD",
            DueDate = due,
            Note = overdue
                ? "Ako ste već platili, označite to u aplikaciji da bi primalac mogao da potvrdi uplatu."
                : "Platite skeniranjem IPS QR koda u aplikaciji vaše banke; novac stiže za nekoliko sekundi.",
        };
        return await SendTemplatedEmailAsync(to, subject, "BillReminderEmail", templateData);
    }

    public async Task<bool> SendOrderConfirmationEmailAsync(string to, string userName, string planName, string orderNumber, decimal amountEur)
    {
        var subject = $"Potvrda narudžbe — {planName}";
        var templateData = new { UserName = userName, PlanName = planName, OrderNumber = orderNumber, AmountEur = amountEur.ToString("F2") };
        return await SendTemplatedEmailAsync(to, subject, "OrderConfirmationEmail", templateData);
    }

}


