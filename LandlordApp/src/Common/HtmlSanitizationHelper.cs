using Ganss.Xss;

namespace Lander.src.Common;

public static class HtmlSanitizationHelper
{
    private static readonly HtmlSanitizer _richTextSanitizer = new();
    private static readonly HtmlSanitizer _plainTextSanitizer = BuildPlainTextSanitizer();

    private static HtmlSanitizer BuildPlainTextSanitizer()
    {
        var s = new HtmlSanitizer();
        s.AllowedTags.Clear();
        s.AllowedAttributes.Clear();
        s.AllowedSchemes.Clear();
        return s;
    }

    public static string? SanitizeRichText(string? input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return _richTextSanitizer.Sanitize(input);
    }

    public static string? SanitizePlainText(string? input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return _plainTextSanitizer.Sanitize(input);
    }
}
