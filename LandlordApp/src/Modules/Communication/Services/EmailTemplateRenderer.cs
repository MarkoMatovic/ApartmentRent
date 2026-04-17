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
        foreach (var prop in templateData.GetType().GetProperties())
        {
            var placeholder = $"{{{{{prop.Name}}}}}";
            var value = prop.GetValue(templateData)?.ToString() ?? "";
            template = template.Replace(placeholder, value);
        }
        return template;
    }
}
