using System.ComponentModel.DataAnnotations;

namespace Lander.Helpers;

public class BrevoSettings
{
    // ApiKey is prod-only required — validated by StartupValidation.ValidateSecrets
    public string ApiKey { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string SenderEmail { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string SenderName { get; set; } = string.Empty;
}
