using Lander;
using Lander.Helpers;
using Lander.src.Modules.Communication.Implementation;
using Lander.src.Modules.Communication.Services;
using Lander.src.Modules.Communication.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using FluentAssertions;

namespace LandlordApp.Tests.Communication;

public class EmailServiceTests
{
    private readonly CommunicationsContext _context;
    private readonly Mock<ILogger<EmailService>> _loggerMock;
    private readonly Mock<IHttpContextAccessor> _httpContextMock;

    public EmailServiceTests()
    {
        var options = new DbContextOptionsBuilder<CommunicationsContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CommunicationsContext(options);
        _loggerMock = new Mock<ILogger<EmailService>>();
        _httpContextMock = new Mock<IHttpContextAccessor>();
        _httpContextMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
    }

    private EmailService CreateService(string apiKey = "test-key")
    {
        var settings = Options.Create(new BrevoSettings
        {
            ApiKey = apiKey,
            SenderEmail = "noreply@landlander.com",
            SenderName = "Landlander Platform"
        });
        return new EmailService(settings, _context, _httpContextMock.Object, new Mock<IEmailTemplateRenderer>().Object, _loggerMock.Object);
    }



    [Fact]
    public async System.Threading.Tasks.Task SendEmailAsync_WithInvalidApiKey_ReturnsFalseAndLogsError()
    {
        var service = CreateService("invalid-key-that-wont-work");

        var result = await service.SendEmailAsync("test@example.com", "Test Subject", "<h1>Test</h1>");

        result.Should().BeFalse();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Brevo")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async System.Threading.Tasks.Task SendEmailAsync_WithEmailToDatabase()
    {
        var service = CreateService("invalid-key");

        await service.SendEmailAsync("tenant@example.com", "Dobrodošli", "<h1>Hello</h1>");

        var log = await _context.EmailLogs.FirstOrDefaultAsync();
        log.Should().NotBeNull();
        log!.RecipientEmail.Should().Be("tenant@example.com");
        log.Subject.Should().Be("Dobrodošli");
        log.IsDelivered.Should().BeFalse();
        log.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async System.Threading.Tasks.Task SendBulkEmailAsync_WithInvalidApiKey_LogsAllRecipients()
    {
        var service = CreateService("invalid-key");
        var recipients = new List<string>
        {
            "user1@example.com",
            "user2@example.com",
            "user3@example.com"
        };

        var result = await service.SendBulkEmailAsync(recipients, "Bulk Test", "<p>Bulk</p>");

        result.Should().BeFalse();
        var logs = await _context.EmailLogs.ToListAsync();
        logs.Should().HaveCount(3);
        logs.Select(l => l.RecipientEmail).Should().BeEquivalentTo(recipients);
        logs.All(l => l.IsDelivered == false).Should().BeTrue();
    }

    [Fact]
    public async System.Threading.Tasks.Task SendWelcomeEmailAsync_WithInvalidApiKey_ReturnsFalse()
    {
        var service = CreateService("invalid-key");

        var result = await service.SendWelcomeEmailAsync("marko@example.com", "Marko");

        result.Should().BeFalse();
        var log = await _context.EmailLogs.FirstOrDefaultAsync();
        log.Should().NotBeNull();
        log!.Subject.Should().Be("Welcome to Landlander Platform!");
    }

    [Fact]
    public async System.Threading.Tasks.Task SendApplicationStatusEmailAsync_WithInvalidApiKey_ReturnsFalse()
    {
        var service = CreateService("invalid-key");

        var result = await service.SendApplicationStatusEmailAsync(
            "tenant@example.com", "Ana", "Jednosoban stan u centru", "Approved");

        result.Should().BeFalse();
        var log = await _context.EmailLogs.FirstOrDefaultAsync();
        log!.Subject.Should().Contain("Jednosoban stan u centru");
    }

    [Fact]
    public async System.Threading.Tasks.Task SendNewMessageEmailAsync_WithInvalidApiKey_SetsCorrectSubject()
    {
        var service = CreateService("invalid-key");

        await service.SendNewMessageEmailAsync("recipient@example.com", "Stefan", "Imate li parking?");

        var log = await _context.EmailLogs.FirstOrDefaultAsync();
        log!.Subject.Should().Be("New Message from Stefan");
    }

    [Fact]
    public async System.Threading.Tasks.Task SendAppointmentConfirmationEmailAsync_WithInvalidApiKey_SetsCorrectSubject()
    {
        var service = CreateService("invalid-key");
        var appointmentDate = new DateTime(2026, 3, 15, 14, 0, 0);

        await service.SendAppointmentConfirmationEmailAsync(
            "tenant@example.com", "Nikola", appointmentDate, "Dvosoban u Novom Beogradu");

        var log = await _context.EmailLogs.FirstOrDefaultAsync();
        log!.Subject.Should().Be("Appointment Confirmation");
    }

    [Fact]
    public async System.Threading.Tasks.Task SendSavedSearchAlertEmailAsync_WithInvalidApiKey_IncludesMatchCount()
    {
        var service = CreateService("invalid-key");

        await service.SendSavedSearchAlertEmailAsync("user@example.com", 5, "Beograd, 2 sobe");

        var log = await _context.EmailLogs.FirstOrDefaultAsync();
        log!.Subject.Should().Contain("5");
    }

    [Fact]
    public async System.Threading.Tasks.Task SendNewApplicationEmailAsync_WithInvalidApiKey_SetsCorrectSubject()
    {
        var service = CreateService("invalid-key");

        await service.SendNewApplicationEmailAsync("landlord@example.com", "Petar", "Trosoban u Zemunu");

        var log = await _context.EmailLogs.FirstOrDefaultAsync();
        log.Should().NotBeNull();
        log!.Subject.Should().Be("New Application for Your Apartment");
        log.RecipientEmail.Should().Be("landlord@example.com");
    }

    [Fact]
    public async System.Threading.Tasks.Task SendListingUnavailableEmailAsync_WithInvalidApiKey_SetsCorrectSubject()
    {
        var service = CreateService("invalid-key");

        await service.SendListingUnavailableEmailAsync("user@example.com", "Jovana", "Jednosoban u Dorćolu", "Iznajmljen");

        var log = await _context.EmailLogs.FirstOrDefaultAsync();
        log.Should().NotBeNull();
        log!.Subject.Should().Contain("Jednosoban u Dorćolu");
        log.RecipientEmail.Should().Be("user@example.com");
    }

    [Fact]
    public async System.Threading.Tasks.Task SendEmailVerificationAsync_WithInvalidApiKey_SetsCorrectSubject()
    {
        var service = CreateService("invalid-key");

        await service.SendEmailVerificationAsync("new@example.com", "Mirna", "https://app.example.com/verify?token=abc");

        var log = await _context.EmailLogs.FirstOrDefaultAsync();
        log.Should().NotBeNull();
        log!.Subject.Should().Be("Verifikacija email adrese");
        log.RecipientEmail.Should().Be("new@example.com");
    }

    [Fact]
    public async System.Threading.Tasks.Task SendPasswordResetEmailAsync_WithInvalidApiKey_SetsCorrectSubject()
    {
        var service = CreateService("invalid-key");

        await service.SendPasswordResetEmailAsync("reset@example.com", "Dragan", "https://app.example.com/reset?token=xyz");

        var log = await _context.EmailLogs.FirstOrDefaultAsync();
        log.Should().NotBeNull();
        log!.Subject.Should().Be("Reset lozinke");
        log.RecipientEmail.Should().Be("reset@example.com");
    }

    [Fact]
    public async System.Threading.Tasks.Task SendTemplatedEmailAsync_WithInvalidApiKey_DelegatesToSendEmailAsync()
    {
        var service = CreateService("invalid-key");

        var result = await service.SendTemplatedEmailAsync(
            "user@example.com", "Custom Subject", "WelcomeEmail", new { UserName = "TestUser" });

        result.Should().BeFalse();
        var log = await _context.EmailLogs.FirstOrDefaultAsync();
        log.Should().NotBeNull();
        log!.Subject.Should().Be("Custom Subject");
    }

    [Fact]
    public void BrevoSettings_DefaultValues_AreEmptyStrings()
    {
        var settings = new BrevoSettings();

        settings.ApiKey.Should().Be(string.Empty);
        settings.SenderEmail.Should().Be(string.Empty);
        settings.SenderName.Should().Be(string.Empty);
    }
}
