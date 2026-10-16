using Lander.Helpers;
using Microsoft.Extensions.Options;
using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;
using SendSmtpEmail = sib_api_v3_sdk.Model.SendSmtpEmail;

namespace Lander.src.Modules.Communication.Services;

/// <summary>The one call to Brevo's transactional e-mail API, so tests can stand in for it without the network.</summary>
public interface IBrevoEmailClient
{
    /// <summary>Sends the e-mail and returns Brevo's message id; throws when Brevo refuses it or cannot be reached.</summary>
    Task<string?> SendAsync(SendSmtpEmail email, CancellationToken ct);
}

public sealed class BrevoEmailClient : IBrevoEmailClient
{
    private readonly BrevoSettings _settings;

    public BrevoEmailClient(IOptions<BrevoSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<string?> SendAsync(SendSmtpEmail email, CancellationToken ct)
    {
        // A configuration per call: the SDK's static Configuration.Default would share the key process-wide.
        var config = new Configuration();
        config.AddApiKey("api-key", _settings.ApiKey ?? string.Empty);
        var api = new TransactionalEmailsApi(config);

        // The SDK's own async call: waiting for Brevo does not hold a thread-pool thread.
        var result = await api.SendTransacEmailAsync(email);
        return result?.MessageId;
    }
}
