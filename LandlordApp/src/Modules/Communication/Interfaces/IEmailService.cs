namespace Lander.src.Modules.Communication.Intefaces;

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
}
