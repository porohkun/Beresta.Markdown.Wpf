namespace MarkdigWpfViewer.Kit;

using System.Text.RegularExpressions;
using Markdig.Helpers;
using Markdig.Parsers;

/// <summary>
/// Разбор <c>[[Страница#Раздел|подпись]]</c> и <c>![[файл|подпись]]</c> в пределах строки. <c>[[_TOC_]]</c> и
/// <c>[[toc]]</c> остаются текстом: это оглавление.
/// </summary>
internal sealed partial class WikiLinkParser : InlineParser
{
    public WikiLinkParser()
    {
        OpeningCharacters = ['[', '!'];
    }

    public override bool Match(InlineProcessor processor, ref StringSlice slice)
    {
        var start = slice.Start;
        var embed = slice.CurrentChar == '!';
        var open = embed ? 3 : 2;
        if (slice.PeekCharExtra(open - 2) != '[' || slice.PeekCharExtra(open - 1) != '[' || start + open > slice.End)
        {
            return false;
        }

        var text = slice.Text;
        var close = text.IndexOf("]]", start + open, slice.End - (start + open) + 1, StringComparison.Ordinal);
        if (close < 0 || Content().Match(text[(start + open)..close]) is not { Success: true } content)
        {
            return false;
        }

        var target = content.Groups["target"].Value.Trim();
        var section = content.Groups["section"].Success ? content.Groups["section"].Value.Trim() : null;
        var label = content.Groups["label"].Success ? content.Groups["label"].Value.Trim() : null;
        if (target.Length == 0 && string.IsNullOrEmpty(section)
            || !embed && section is null && label is null && (target == "_TOC_" || target.Equals("toc", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var position = processor.GetSourcePosition(start, out var line, out var column);
        processor.Inline = new WikiLinkInline
        {
            Target = target,
            Section = section,
            Label = label,
            IsEmbed = embed,
            Span = new(position, position + close + 1 - start),
            Line = line,
            Column = column
        };

        slice.Start = close + 2;
        return true;
    }

    [GeneratedRegex(@"^(?<target>[^#|\[\]\n]*)(?:#(?<section>[^|\[\]\n]*))?(?:\|(?<label>[^\[\]\n]*))?$")]
    private static partial Regex Content();
}
