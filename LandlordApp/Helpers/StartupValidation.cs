namespace Lander.Helpers;

public static class StartupValidation
{
    private static readonly HashSet<string> Placeholders = new(StringComparer.OrdinalIgnoreCase)
    {
        "REPLACE_WITH_YOUR_AUTHENTICITY_TOKEN",
        "REPLACE_WITH_YOUR_MERCHANT_KEY",
        "YOUR_SENDGRID_API_KEY_HERE",
        "YOUR_BREVO_API_KEY_HERE",
        "YourSuperSecretKeyForJWTTokenGenerationMustBeAtLeast32CharactersLong",
        "sk_test_replace_me",
        "whsec_replace_me",
    };

    private static readonly string[] RequiredKeys =
    [
        "Jwt:Secret",
        "Jwt:Issuer",
        "Jwt:Audience",
        "Brevo:ApiKey",       // actual email provider used by EmailService
        "Monri:AuthenticityToken",
        "Monri:MerchantKey",
        "ConnectionStrings:DefaultConnection",
    ];

    public static void ValidateSecrets(IConfiguration configuration, IWebHostEnvironment env)
    {
        if (env.IsDevelopment()) return; // development koristi dotnet user-secrets

        var missing = new List<string>();
        foreach (var key in RequiredKeys)
        {
            var value = configuration[key];
            if (string.IsNullOrWhiteSpace(value) || Placeholders.Contains(value))
                missing.Add(key);
        }

        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"The following configuration keys are missing or contain placeholder values: " +
                $"{string.Join(", ", missing)}. " +
                "Set them via environment variables (e.g. Jwt__Secret=...) or Azure App Service Configuration.");
    }
}
