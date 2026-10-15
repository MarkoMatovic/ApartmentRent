using System.Text.RegularExpressions;
using FluentAssertions;
using Lander.src.Modules.Communication.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace LandlordApp.Tests.Communication;

/// <summary>Guards the wording and the links of the e-mails users actually receive.</summary>
public class EmailTemplateTests
{
    private static string TemplateDirectory()
    {
        // Walk up from the test output to the repo so the real files are checked, not a stale copy.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "LandlordApp", "src", "Modules", "Communication", "EmailTemplates");
            if (Directory.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("EmailTemplates folder not found");
    }

    public static IEnumerable<object[]> Templates() =>
        Directory.GetFiles(TemplateDirectory(), "*.html").Select(f => new object[] { Path.GetFileName(f) });

    // Ijekavian / unaccented forms that used to appear. The audience writes in ekavian with diacritics.
    private static readonly Regex Forbidden = new(
        @"cijen|vrijem|mjest|uspje|obavje|zahtjev|nalijep|nepromijen|promijen|unaprijed|dosli|sacuv|pretrazivati|pretrazite|pridruzil|mozete|zelite|vase|vasa|vasoj|nacin|tacn",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    [Theory]
    [MemberData(nameof(Templates))]
    public void Template_is_ekavian_with_diacritics(string fileName)
    {
        // only the visible text matters, not the CSS / attributes
        var html = File.ReadAllText(Path.Combine(TemplateDirectory(), fileName));
        var text = Regex.Replace(Regex.Replace(html, "(?s)<style.*?</style>", " "), "<[^>]+>", " ");

        Forbidden.Matches(text).Select(m => m.Value).Should().BeEmpty($"{fileName} must be in ekavian Serbian with diacritics");
    }

    [Theory]
    [MemberData(nameof(Templates))]
    public void Template_uses_the_configured_site_address_and_no_hard_coded_domain(string fileName)
    {
        var html = File.ReadAllText(Path.Combine(TemplateDirectory(), fileName));

        html.Should().NotContain("https://turentaj.com", "links must follow App:FrontendBaseUrl via {{BaseUrl}}");
    }

    [Theory]
    [MemberData(nameof(Templates))]
    public void Template_links_point_at_routes_that_exist_in_the_app(string fileName)
    {
        var html = File.ReadAllText(Path.Combine(TemplateDirectory(), fileName));
        var known = new[]
        {
            "/applications/sent", "/applications/received", "/appointments/my", "/chat", "/apartments",
            "/istorija-placanja", "/roommates", "/messages", "/my-roommate", "/bills",
        };

        foreach (System.Text.RegularExpressions.Match m in Regex.Matches(html, @"\{\{BaseUrl\}\}(/[a-z/\-]*)?"))
        {
            var path = m.Groups[1].Success ? m.Groups[1].Value : "";
            if (path.Length == 0) continue; // site root
            known.Should().Contain(path, $"{fileName} links to {path}");
        }
    }

    [Fact]
    public void Every_template_the_email_service_asks_for_exists()
    {
        var wanted = new[]
        {
            "WelcomeEmail", "NewApplicationEmail", "ApplicationStatusEmail", "NewMessageEmail",
            "AppointmentConfirmationEmail", "SavedSearchAlertEmail", "ListingUnavailableEmail",
            "EmailVerificationEmail", "PasswordResetEmail", "OrderConfirmationEmail",
            "RoommateProposalEmail", "RoommateAcceptedEmail",
        };

        foreach (var name in wanted)
            File.Exists(Path.Combine(TemplateDirectory(), name + ".html")).Should().BeTrue(name);
    }

    [Fact]
    public void Rendered_welcome_mail_has_the_name_the_real_site_link_and_no_leftover_placeholders()
    {
        var renderer = new EmailTemplateRenderer(
            new Mock<ILogger<EmailTemplateRenderer>>().Object,
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["App:FrontendBaseUrl"] = "https://moj-sajt.example/" })
                .Build());

        var html = renderer.Render("WelcomeEmail", new { UserName = "Marko <b>Matović</b>" });

        html.Should().Contain("Dobrodošli na TuRentaj!");
        html.Should().Contain("href=\"https://moj-sajt.example\"", "the trailing slash is trimmed");
        html.Should().Contain("Marko &lt;b&gt;Matović&lt;/b&gt;", "user input is HTML-encoded");
        html.Should().NotContain("{{");
    }
}
