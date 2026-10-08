using FluentAssertions;
using Lander.src.Common;
using Xunit;

namespace LandlordApp.Tests.Services;

/// <summary>
/// HtmlSanitizationHelper is the only thing between user-written text (listing descriptions, bios,
/// search requests) and other users' browsers, and it had no tests at all. These pin its behaviour
/// with real XSS payloads — including the mutation-XSS (mXSS) shapes behind the HtmlSanitizer /
/// AngleSharp advisories (GHSA-j92c-7v7g-gj3f template-tag bypass, GHSA-pgww-w46g-26qg
/// annotation-xml) — so a future parser or package change cannot quietly weaken it.
/// </summary>
public class HtmlSanitizationHelperTests
{
    public static IEnumerable<object[]> XssPayloads => new[]
    {
        "<script>alert(1)</script>",
        "<img src=x onerror=alert(1)>",
        "<svg onload=alert(1)>",
        "<a href=\"javascript:alert(1)\">click</a>",
        "<a href=\"  JaVaScRiPt:alert(1)\">click</a>",
        "<iframe src=\"https://evil.example\"></iframe>",
        "<body onload=alert(1)>",
        "<div style=\"background:url(javascript:alert(1))\">x</div>",
        "<object data=\"javascript:alert(1)\"></object>",
        // mutation-XSS shapes (parse differently the second time round)
        "<template><img src=x onerror=alert(1)></template>",
        "<template><script>alert(1)</script></template>",
        "<math><annotation-xml encoding=\"text/html\"><style><img src=x onerror=alert(1)></style></annotation-xml></math>",
        "<form><math><mtext></form><form><mglyph><style></math><img src onerror=alert(1)>",
        "<noscript><p title=\"</noscript><img src=x onerror=alert(1)>\">",
        "<svg><style><img src=x onerror=alert(1)></style></svg>",
        "<math><mi><style><img src=x onerror=alert(1)></style></mi></math>",
    }.Select(p => new object[] { p });

    private static void ShouldContainNothingExecutable(string? html)
    {
        html ??= string.Empty;
        html.Should().NotContainEquivalentOf("<script");
        html.Should().NotContainEquivalentOf("onerror");
        html.Should().NotContainEquivalentOf("onload");
        html.Should().NotContainEquivalentOf("javascript:");
        html.Should().NotContainEquivalentOf("<iframe");
        html.Should().NotContainEquivalentOf("<object");
    }

    [Theory]
    [MemberData(nameof(XssPayloads))]
    public void RichText_StripsExecutableContent(string payload)
    {
        ShouldContainNothingExecutable(HtmlSanitizationHelper.SanitizeRichText(payload));
    }

    [Theory]
    [MemberData(nameof(XssPayloads))]
    public void PlainText_StripsExecutableContent(string payload)
    {
        ShouldContainNothingExecutable(HtmlSanitizationHelper.SanitizePlainText(payload));
    }

    [Theory]
    [MemberData(nameof(XssPayloads))]
    public void RichText_IsStableWhenSanitizedTwice(string payload)
    {
        // mXSS works by output that re-parses into something different. Safe output must be a fixed point.
        var once = HtmlSanitizationHelper.SanitizeRichText(payload);
        var twice = HtmlSanitizationHelper.SanitizeRichText(once);

        twice.Should().Be(once);
    }

    [Fact]
    public void RichText_KeepsHarmlessFormatting()
    {
        var result = HtmlSanitizationHelper.SanitizeRichText("<p>Sunny <b>2-room</b> flat, <i>quiet</i> street.</p>");

        result.Should().Contain("<b>2-room</b>").And.Contain("<i>quiet</i>").And.Contain("Sunny");
    }

    [Fact]
    public void PlainText_RemovesMarkup_AndKeepsTextThatIsOutsideIt()
    {
        var result = HtmlSanitizationHelper.SanitizePlainText("Sunny flat <b>bold</b> near the park");

        result.Should().NotContain("<").And.Contain("Sunny flat").And.Contain("near the park");
    }

    [Fact]
    public void PlainText_DropsAnElementTogetherWithItsContent_NotJustTheTag()
    {
        // Documents existing behaviour (HtmlSanitizer's KeepChildNodes defaults to false): text that a
        // user wraps in markup disappears rather than being unwrapped. Unchanged by the 8.x -> 9.2 upgrade.
        // If keeping the inner text is ever wanted, set KeepChildNodes = true on the plain-text sanitizer.
        HtmlSanitizationHelper.SanitizePlainText("<p>Sunny flat</p>").Should().BeEmpty();
    }

    [Fact]
    public void PlainText_DoesNotBreakOrdinaryPunctuationAndDiacritics()
    {
        const string text = "Stan od 45 m², Novi Sad (Liman) — šetnja do centra, cena: 350€ + račun";

        HtmlSanitizationHelper.SanitizePlainText(text).Should().Be(text);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NullAndEmpty_PassThroughUnchanged(string? input)
    {
        HtmlSanitizationHelper.SanitizeRichText(input).Should().Be(input);
        HtmlSanitizationHelper.SanitizePlainText(input).Should().Be(input);
    }
}
