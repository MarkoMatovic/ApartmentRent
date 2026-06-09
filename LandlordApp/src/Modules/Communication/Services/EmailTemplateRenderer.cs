using System.Net;
using System.Reflection;

namespace Lander.src.Modules.Communication.Services;

public class EmailTemplateRenderer : IEmailTemplateRenderer
{
    private readonly ILogger<EmailTemplateRenderer> _logger;

    public EmailTemplateRenderer(ILogger<EmailTemplateRenderer> logger)
    {
        _logger = logger;
    }

    public string Render(string templateName, object templateData)
    {
        var templatePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "src", "Modules", "Communication", "EmailTemplates",
            $"{templateName}.html");

        if (!File.Exists(templatePath))
        {
            _logger.LogWarning("Template {TemplateName} not found at {Path}", templateName, templatePath);
            return $"<h1>Template {templateName} not found</h1>";
        }

        var template = File.ReadAllText(templatePath);

        // HTML-encode every placeholder value before injecting into the template.
        // Without encoding, a user-controlled field (name, message preview, etc.)
        // could inject arbitrary HTML/JS into the outgoing email.
        foreach (var prop in templateData.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var placeholder = $"{{{{{prop.Name}}}}}";
            var raw = prop.GetValue(templateData)?.ToString() ?? "";
            var encoded = WebUtility.HtmlEncode(raw);
            template = template.Replace(placeholder, encoded);
        }
        return template;
    }
}
