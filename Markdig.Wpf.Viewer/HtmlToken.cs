namespace MarkdigWpfViewer;

using System.Text.RegularExpressions;

/// <summary>
/// Кусок HTML: тег, комментарий или текст между ними, с позицией в разобранной строке. У тега,
/// который не разобрался, <see cref="Tag"/> пуст, а <see cref="IsTag"/> — да.
/// </summary>
internal sealed partial record HtmlToken(HtmlTag? Tag, string Text, int Start, bool IsTag, bool IsComment)
{
    public int End => Start + Text.Length;

    /// <summary>Теги, комментарии и текст строки по порядку.</summary>
    public static List<HtmlToken> Split(string html)
    {
        var tokens = new List<HtmlToken>();
        var position = 0;

        foreach (Match match in MarkupPattern().Matches(html))
        {
            if (match.Index > position)
            {
                tokens.Add(new(null, html[position..match.Index], position, IsTag: false, IsComment: false));
            }

            var comment = match.Value.StartsWith("<!--", StringComparison.Ordinal);
            tokens.Add(new(comment ? null : HtmlTag.Parse(match.Value), match.Value, match.Index, IsTag: !comment, comment));
            position = match.Index + match.Length;
        }

        if (position < html.Length)
        {
            tokens.Add(new(null, html[position..], position, IsTag: false, IsComment: false));
        }

        return tokens;
    }

    [GeneratedRegex(@"<!--[\s\S]*?-->|<[/!?a-zA-Z][^<>]*>")]
    private static partial Regex MarkupPattern();
}
