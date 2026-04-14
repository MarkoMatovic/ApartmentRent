using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Lander;
using Lander.Helpers;
using Lander.src.Modules.Communication.Dtos.InputDto;
using Lander.src.Modules.Communication.Implementation;

namespace LandlordApp.Tests.Services;

public class SmsServiceTests : IDisposable
{
    private readonly CommunicationsContext _context;
    private readonly IOptions<TwilioSettings> _twilioOptions;
    private readonly SmsService _smsService;

    public SmsServiceTests()
    {
        var options = new DbContextOptionsBuilder<CommunicationsContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new CommunicationsContext(options);

        _twilioOptions = Options.Create(new TwilioSettings
        {
            AccountSid = "test_account_sid",
            AuthToken = "test_auth_token",
            PhoneNumber = "+1234567890"
        });

        _smsService = new SmsService(_twilioOptions, _context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    // ─── Constructor null checks ──────────────────────────────────────────────

    [Fact]
    public void Constructor_NullSettings_ThrowsArgumentNullException()
    {
        IOptions<TwilioSettings> nullSettings = Options.Create<TwilioSettings>(null!);

        var act = () => new SmsService(nullSettings, _context);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullContext_ThrowsArgumentNullException()
    {
        var act = () => new SmsService(_twilioOptions, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    // ─── SendSmsAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task SendSmsAsync_InvalidTwilioConfig_ThrowsException()
    {
        // Twilio credentials are fake — the call will always fail in test environment.
        // This verifies exception propagation from the Twilio layer.
        var dto = new SendSmsInputDto
        {
            ToPhoneNumber = "+381601234567",
            MessageText = "Test message",
            SenderId = 1,
            ReceiverId = 2
        };

        await Assert.ThrowsAnyAsync<Exception>(() => _smsService.SendSmsAsync(dto));
    }
}
