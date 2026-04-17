namespace Lander.src.Modules.Communication.Services;

public interface IEmailTemplateRenderer
{
    string Render(string templateName, object templateData);
}
