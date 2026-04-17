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

    /// <summary>
    /// Sanitizes HTML input allowing safe formatting tags (for apartment descriptions).
    /// </summary>
    public static string? SanitizeRichText(string? input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return _richTextSanitizer.Sanitize(input);
    }

    /// <summary>
    /// Strips all HTML tags, returning plain text (for bio, title, address fields).
    /// </summary>
    public static string? SanitizePlainText(string? input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return _plainTextSanitizer.Sanitize(input);
    }
}
