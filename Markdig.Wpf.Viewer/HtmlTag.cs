namespace MarkdigWpfViewer;

using System.Net;
using System.Text.RegularExpressions;

/// <summary>Строчный HTML-тег: имя, открывающий или закрывающий, атрибуты.</summary>
internal sealed partial record HtmlTag(string Name, bool IsClosing, bool IsSelfClosing, IReadOnlyDictionary<string, string> Attributes)
{
    /// <summary>Разбирает тег; комментарий и не тег — <c>null</c>.</summary>
    public static HtmlTag? Parse(string tag)
    {
        var match = TagPattern().Match(tag);
        if (!match.Success)
        {
            return null;
        }

        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match attribute in AttributePattern().Matches(match.Groups["attributes"].Value))
        {
            attributes.TryAdd(attribute.Groups["name"].Value, WebUtility.HtmlDecode(attribute.Groups["value"].Value));
        }

        return new(
            match.Groups["name"].Value.ToLowerInvariant(),
            match.Groups["closing"].Success,
            match.Groups["self"].Success,
            attributes);
    }

    [GeneratedRegex(@"^<(?<closing>/)?(?<name>[a-zA-Z][a-zA-Z0-9]*)(?<attributes>(?:\s[^>]*?)?)\s*(?<self>/)?>$")]
    private static partial Regex TagPattern();

    [GeneratedRegex("""(?<name>[a-zA-Z_:][-a-zA-Z0-9_:.]*)\s*(?:=\s*(?:"(?<value>[^"]*)"|'(?<value>[^']*)'|(?<value>[^\s"'=<>`]+)))?""")]
    private static partial Regex AttributePattern();
}
