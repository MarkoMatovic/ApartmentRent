namespace Lander.src.Modules.Communication.Interfaces;
public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string htmlContent);
    Task<bool> SendTemplatedEmailAsync(string to, string subject, string templateName, object templateData);
    Task<bool> SendBulkEmailAsync(List<string> recipients, string subject, string htmlContent);
    Task<bool> SendWelcomeEmailAsync(string to, string userName);
    Task<bool> SendNewApplicationEmailAsync(string to, string landlordName, string apartmentTitle);
    Task<bool> SendApplicationStatusEmailAsync(string to, string tenantName, string apartmentTitle, string status);
    Task<bool> SendNewMessageEmailAsync(string to, string senderName, string messagePreview);
    Task<bool> SendAppointmentConfirmationEmailAsync(string to, string userName, DateTime appointmentDate, string apartmentTitle);
    Task<bool> SendSavedSearchAlertEmailAsync(string to, int matchCount, string searchCriteria);
    Task<bool> SendListingUnavailableEmailAsync(string to, string userName, string apartmentTitle, string reason);
    Task<bool> SendEmailVerificationAsync(string to, string userName, string verificationLink);
    Task<bool> SendPasswordResetEmailAsync(string to, string userName, string resetLink);
    Task<bool> SendRoommateProposalEmailAsync(string to, string userName, string proposerName);
    Task<bool> SendRoommateAcceptedEmailAsync(string to, string userName, string partnerName);
    /// <summary>A bill is due in a few days, or (<paramref name="overdue"/>) its due date has passed.</summary>
    Task<bool> SendBillReminderEmailAsync(string to, string userName, string billTitle, decimal amount, DateOnly dueDate, bool overdue);
    Task<bool> SendOrderConfirmationEmailAsync(string to, string userName, string planName, string orderNumber, decimal amountEur);
}
