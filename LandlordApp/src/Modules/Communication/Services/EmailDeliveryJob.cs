using Hangfire;
using Hangfire.Server;
using Lander.Helpers;
using Lander.src.Modules.Communication.Models;
using Microsoft.Extensions.Options;
using SendSmtpEmail = sib_api_v3_sdk.Model.SendSmtpEmail;
using SendSmtpEmailSender = sib_api_v3_sdk.Model.SendSmtpEmailSender;
using SendSmtpEmailTo = sib_api_v3_sdk.Model.SendSmtpEmailTo;

namespace Lander.src.Modules.Communication.Services;

/// <summary>An e-mail waiting to be sent: what the delivery job needs, nothing tied to the request that made it.</summary>
public sealed class QueuedEmail
{
    public List<string> To { get; set; } = new();
    public string Subject { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    /// <summary>The signed-in user whose action produced the e-mail, for the log.</summary>
    public Guid? RequestedBy { get; set; }
}

/// <summary>
/// Sends a queued e-mail through Brevo as a Hangfire job, so no request waits for Brevo (with the
/// retries inside the request, a message waited 14 s whenever Brevo was unreachable). When Brevo refuses or cannot be reached,
/// Hangfire tries again after 1, 5 and 15 minutes; the outcome is written to EmailLogs once, after the
/// delivery or after the last attempt. A job that still fails stays in Hangfire's "Failed" list.
/// </summary>
public class EmailDeliveryJob
{
    public const int Retries = 3;

    private readonly BrevoSettings _settings;
    private readonly IBrevoEmailClient _brevo;
    private readonly CommunicationsContext _context;
    private readonly ILogger<EmailDeliveryJob> _logger;

    public EmailDeliveryJob(IOptions<BrevoSettings> settings, IBrevoEmailClient brevo, CommunicationsContext context,
        ILogger<EmailDeliveryJob> logger)
    {
        _settings = settings.Value;
        _brevo = brevo;
        _context = context;
        _logger = logger;
    }

    /// <param name="context">Filled in by Hangfire; null outside a job (tests), which counts as the last attempt.</param>
    [AutomaticRetry(Attempts = Retries, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task SendAsync(QueuedEmail email, PerformContext? context)
    {
        var message = new SendSmtpEmail(
            sender: new SendSmtpEmailSender(_settings.SenderName, _settings.SenderEmail),
            to: email.To.Select(address => new SendSmtpEmailTo(address)).ToList(),
            subject: email.Subject,
            htmlContent: email.HtmlContent);

        string? messageId;
        try
        {
            messageId = await _brevo.SendAsync(message, CancellationToken.None);
        }
        catch (Exception ex)
        {
            var attempt = context?.GetJobParameter<int>("RetryCount") ?? Retries;
            if (attempt < Retries)
            {
                _logger.LogWarning(ex, "Brevo: sending to {To} failed (attempt {Attempt}), Hangfire will try again",
                    string.Join(", ", email.To), attempt + 1);
                throw;
            }

            _logger.LogError(ex, "Brevo: Failed to send email to {To}", string.Join(", ", email.To));
            await EmailLogWriter.WriteAsync(_context, _logger, email.To, email.Subject, email.RequestedBy, false, null, ex.Message);
            throw;
        }

        await EmailLogWriter.WriteAsync(_context, _logger, email.To, email.Subject, email.RequestedBy, true, messageId, null);
    }
}

/// <summary>One EmailLogs row per recipient. The body is never stored (size, and links with tokens in it).</summary>
public static class EmailLogWriter
{
    public static async Task WriteAsync(CommunicationsContext context, ILogger logger, IEnumerable<string> recipients,
        string subject, Guid? requestedBy, bool delivered, string? providerMessageId, string? error)
    {
        var now = DateTime.UtcNow;
        foreach (var recipient in recipients)
        {
            context.EmailLogs.Add(new EmailLog
            {
                RecipientEmail = recipient,
                Subject = subject,
                HtmlContent = null,
                SentAt = now,
                IsDelivered = delivered,
                ProviderMessageId = providerMessageId,
                ErrorMessage = error,
                CreatedByGuid = requestedBy,
                CreatedDate = now,
            });
        }
        try
        {
            await context.SaveEntitiesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to log email to database");
        }
    }
}
