using System.Linq.Expressions;
using Lander;
using Lander.Helpers;
using Lander.src.Infrastructure.Services;
using Lander.src.Modules.Communication.Implementation;
using Lander.src.Modules.Communication.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using FluentAssertions;
using SendSmtpEmail = sib_api_v3_sdk.Model.SendSmtpEmail;

namespace LandlordApp.Tests.Communication;

public class EmailServiceTests
{
    private readonly CommunicationsContext _context;
    private readonly Mock<ILogger<EmailService>> _loggerMock;
    private readonly Mock<IHttpContextAccessor> _httpContextMock;
    private readonly FakeScheduler _scheduler = new();
    private readonly FakeBrevo _brevo = new();

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

    /// <summary>Stands in for Brevo: no network, and it refuses until told to accept.</summary>
    private sealed class FakeBrevo : IBrevoEmailClient
    {
        public bool Accepts { get; set; }
        public List<SendSmtpEmail> Sent { get; } = new();

        public Task<string?> SendAsync(SendSmtpEmail email, CancellationToken ct)
        {
            if (!Accepts) throw new sib_api_v3_sdk.Client.ApiException(401, "Brevo refused the key");
            Sent.Add(email);
            return Task.FromResult<string?>("<msg-1@brevo>");
        }
    }

    /// <summary>Keeps the jobs instead of handing them to Hangfire, so a test can look at them or run them.</summary>
    private sealed class FakeScheduler : IBackgroundScheduler
    {
        public List<LambdaExpression> Jobs { get; } = new();
        public bool Fails { get; set; }

        public void Enqueue<T>(Expression<Action<T>> job) => Add(job);
        public void Enqueue<T>(Expression<Func<T, Task>> job) => Add(job);
        public void Schedule<T>(TimeSpan delay, Expression<Action<T>> job) => Add(job);

        private void Add(LambdaExpression job)
        {
            if (Fails) throw new InvalidOperationException("Hangfire storage is not reachable");
            Jobs.Add(job);
        }

        /// <summary>The e-mails handed to <see cref="EmailDeliveryJob.SendAsync"/>, in order.</summary>
        public List<QueuedEmail> Emails => Jobs
            .Select(j => (MethodCallExpression)j.Body)
            .Select(call => (QueuedEmail)Expression.Lambda(call.Arguments[0]).Compile().DynamicInvoke()!)
            .ToList();

        public async Task RunAsync(EmailDeliveryJob job)
        {
            foreach (var expression in Jobs.Cast<Expression<Func<EmailDeliveryJob, Task>>>())
                await expression.Compile()(job);
        }
    }

    private EmailService CreateService(string apiKey = "test-key")
    {
        var settings = Options.Create(new BrevoSettings
        {
            ApiKey = apiKey,
            SenderEmail = "noreply@landlander.com",
            SenderName = "Landlander Platform"
        });
        return new EmailService(settings, _context, _httpContextMock.Object, new Mock<IEmailTemplateRenderer>().Object,
            _loggerMock.Object, _scheduler);
    }

    private EmailDeliveryJob CreateJob() => new(
        Options.Create(new BrevoSettings { ApiKey = "test-key", SenderEmail = "noreply@landlander.com", SenderName = "Landlander Platform" }),
        _brevo, _context, NullLogger<EmailDeliveryJob>.Instance);

    [Fact]
    public async Task SendEmailAsync_OnlyQueuesTheMail_BrevoIsCalledByTheJob()
    {
        _brevo.Accepts = true;
        var service = CreateService();

        var result = await service.SendEmailAsync("test@example.com", "Test Subject", "<h1>Test</h1>");

        result.Should().BeTrue();
        _brevo.Sent.Should().BeEmpty();
        (await _context.EmailLogs.CountAsync()).Should().Be(0);
        var queued = _scheduler.Emails.Should().ContainSingle().Subject;
        queued.To.Should().Equal("test@example.com");
        queued.Subject.Should().Be("Test Subject");
        queued.HtmlContent.Should().Be("<h1>Test</h1>");
    }

    [Fact]
    public async Task DeliveryJob_WhenBrevoAccepts_SendsTheMailAndLogsTheDelivery()
    {
        _brevo.Accepts = true;
        await CreateService().SendEmailAsync("test@example.com", "Test Subject", "<h1>Test</h1>");

        await _scheduler.RunAsync(CreateJob());

        var sent = _brevo.Sent.Should().ContainSingle().Subject;
        sent.To.Should().ContainSingle().Which.Email.Should().Be("test@example.com");
        sent.Sender.Email.Should().Be("noreply@landlander.com");
        sent.Subject.Should().Be("Test Subject");
        var log = await _context.EmailLogs.SingleAsync();
        log.IsDelivered.Should().BeTrue();
        log.ProviderMessageId.Should().Be("<msg-1@brevo>");
        log.HtmlContent.Should().BeNull();
    }

    [Fact]
    public async Task DeliveryJob_WhenBrevoRefusesOnTheLastAttempt_LogsTheErrorAndFailsTheJob()
    {
        await CreateService().SendEmailAsync("tenant@example.com", "Dobrodošli", "<h1>Hello</h1>");

        var run = () => _scheduler.RunAsync(CreateJob());

        await run.Should().ThrowAsync<sib_api_v3_sdk.Client.ApiException>();
        var log = await _context.EmailLogs.SingleAsync();
        log.RecipientEmail.Should().Be("tenant@example.com");
        log.Subject.Should().Be("Dobrodošli");
        log.IsDelivered.Should().BeFalse();
        log.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SendEmailAsync_WithoutAnApiKey_QueuesNothing_AndLogsItAsNotDelivered()
    {
        var service = CreateService(apiKey: "");

        var single = await service.SendEmailAsync("a@example.com", "S", "<p>x</p>");
        var bulk = await service.SendBulkEmailAsync(new List<string> { "b@example.com", "c@example.com" }, "S", "<p>x</p>");

        single.Should().BeFalse();
        bulk.Should().BeFalse();
        _scheduler.Jobs.Should().BeEmpty();
        var logs = await _context.EmailLogs.ToListAsync();
        logs.Should().HaveCount(3).And.OnlyContain(l => l.IsDelivered == false && l.ErrorMessage == "Brevo API key is not configured");
    }

    [Fact]
    public async Task SendEmailAsync_WhenTheQueueIsNotReachable_ReturnsFalseAndLogsIt()
    {
        _scheduler.Fails = true;

        var result = await CreateService().SendEmailAsync("test@example.com", "Test Subject", "<h1>Test</h1>");

        result.Should().BeFalse();
        var log = await _context.EmailLogs.SingleAsync();
        log.IsDelivered.Should().BeFalse();
        log.ErrorMessage.Should().Contain("Hangfire");
    }

    [Fact]
    public async Task SendBulkEmailAsync_SendsOneMailToAllAndLogsEach()
    {
        _brevo.Accepts = true;
        var result = await CreateService().SendBulkEmailAsync(new List<string> { "a@example.com", "b@example.com" }, "Bulk", "<p>x</p>");

        await _scheduler.RunAsync(CreateJob());

        result.Should().BeTrue();
        _brevo.Sent.Should().ContainSingle().Which.To.Select(t => t.Email).Should().Equal("a@example.com", "b@example.com");
        (await _context.EmailLogs.CountAsync(l => l.IsDelivered == true)).Should().Be(2);
    }

    [Fact]
    public async Task SendBulkEmailAsync_WhenBrevoRefuses_LogsAllRecipients()
    {
        var recipients = new List<string> { "user1@example.com", "user2@example.com", "user3@example.com" };
        await CreateService().SendBulkEmailAsync(recipients, "Bulk Test", "<p>Bulk</p>");

        var run = () => _scheduler.RunAsync(CreateJob());

        await run.Should().ThrowAsync<sib_api_v3_sdk.Client.ApiException>();
        var logs = await _context.EmailLogs.ToListAsync();
        logs.Select(l => l.RecipientEmail).Should().BeEquivalentTo(recipients);
        logs.Should().OnlyContain(l => l.IsDelivered == false);
    }

    private async Task<QueuedEmail> QueuedBy(Func<EmailService, Task<bool>> send)
    {
        (await send(CreateService())).Should().BeTrue();
        return _scheduler.Emails.Should().ContainSingle().Subject;
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_SetsTheSubject()
    {
        var email = await QueuedBy(s => s.SendWelcomeEmailAsync("marko@example.com", "Marko"));
        email.Subject.Should().Be("Dobrodošli na TuRentaj!");
        email.To.Should().Equal("marko@example.com");
    }

    [Fact]
    public async Task SendApplicationStatusEmailAsync_NamesTheApartment()
    {
        var email = await QueuedBy(s => s.SendApplicationStatusEmailAsync("tenant@example.com", "Ana", "Jednosoban stan u centru", "Approved"));
        email.Subject.Should().Contain("Jednosoban stan u centru");
    }

    [Fact]
    public async Task SendNewMessageEmailAsync_NamesTheSender()
    {
        var email = await QueuedBy(s => s.SendNewMessageEmailAsync("recipient@example.com", "Stefan", "Imate li parking?"));
        email.Subject.Should().Be("Nova poruka od Stefan");
    }

    [Fact]
    public async Task SendAppointmentConfirmationEmailAsync_SetsTheSubject()
    {
        var email = await QueuedBy(s => s.SendAppointmentConfirmationEmailAsync(
            "tenant@example.com", "Nikola", new DateTime(2026, 3, 15, 14, 0, 0), "Dvosoban u Novom Beogradu"));
        email.Subject.Should().Be("Potvrda razgledanja stana");
    }

    [Fact]
    public async Task SendSavedSearchAlertEmailAsync_IncludesTheMatchCount()
    {
        var email = await QueuedBy(s => s.SendSavedSearchAlertEmailAsync("user@example.com", 5, "Beograd, 2 sobe"));
        email.Subject.Should().Contain("5");
    }

    [Fact]
    public async Task SendNewApplicationEmailAsync_SetsTheSubject()
    {
        var email = await QueuedBy(s => s.SendNewApplicationEmailAsync("landlord@example.com", "Petar", "Trosoban u Zemunu"));
        email.Subject.Should().Be("Nova prijava za vaš stan");
        email.To.Should().Equal("landlord@example.com");
    }

    [Fact]
    public async Task SendRoommateProposalEmailAsync_NamesTheProposerInTheSubject()
    {
        var email = await QueuedBy(s => s.SendRoommateProposalEmailAsync("marko@example.com", "Marko", "Ana Anić"));
        email.Subject.Should().Be("Ana Anić vam predlaže da budete cimeri");
        email.To.Should().Equal("marko@example.com");
    }

    [Fact]
    public async Task SendRoommateAcceptedEmailAsync_SetsTheSubject()
    {
        var email = await QueuedBy(s => s.SendRoommateAcceptedEmailAsync("ana@example.com", "Ana", "Marko Marković"));
        email.Subject.Should().Be("Cimerstvo je potvrđeno");
    }

    [Fact]
    public async Task SendListingUnavailableEmailAsync_NamesTheApartment()
    {
        var email = await QueuedBy(s => s.SendListingUnavailableEmailAsync("user@example.com", "Jovana", "Jednosoban u Dorćolu", "Iznajmljen"));
        email.Subject.Should().Contain("Jednosoban u Dorćolu");
        email.To.Should().Equal("user@example.com");
    }

    [Fact]
    public async Task SendEmailVerificationAsync_SetsTheSubject()
    {
        var email = await QueuedBy(s => s.SendEmailVerificationAsync("new@example.com", "Mirna", "https://app.example.com/verify?token=abc"));
        email.Subject.Should().Be("Verifikacija email adrese");
        email.To.Should().Equal("new@example.com");
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_SetsTheSubject()
    {
        var email = await QueuedBy(s => s.SendPasswordResetEmailAsync("reset@example.com", "Dragan", "https://app.example.com/reset?token=xyz"));
        email.Subject.Should().Be("Reset lozinke");
        email.To.Should().Equal("reset@example.com");
    }

    [Fact]
    public async Task SendTemplatedEmailAsync_UsesTheGivenSubject()
    {
        var email = await QueuedBy(s => s.SendTemplatedEmailAsync("user@example.com", "Custom Subject", "WelcomeEmail", new { UserName = "TestUser" }));
        email.Subject.Should().Be("Custom Subject");
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
