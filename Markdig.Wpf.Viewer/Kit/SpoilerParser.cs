namespace MarkdigWpfViewer.Kit;

using System.Text.RegularExpressions;
using Markdig.Helpers;
using Markdig.Parsers;

/// <summary>
/// Разбор спойлера <c>||текст||</c> в пределах строки. В строке с одиночной <c>|</c> — это строка
/// таблицы, где <c>|</c> делит ячейки, — спойлер не разбирается.
/// </summary>
internal sealed partial class SpoilerParser : InlineParser
{
    public SpoilerParser()
    {
        OpeningCharacters = ['|'];
    }

    public override bool Match(InlineProcessor processor, ref StringSlice slice)
    {
        var start = slice.Start;
        var text = slice.Text;
        if (slice.PeekCharExtra(1) != '|' || slice.PeekCharExtra(2) is '|' or ' ' or '\0' or '\n')
        {
            return false;
        }

        var lineStart = text.LastIndexOf('\n', start) + 1;
        var lineEnd = text.IndexOf('\n', start) is var newline and >= 0 ? newline : text.Length;
        var end = text.IndexOf("||", start + 2, lineEnd - (start + 2), StringComparison.Ordinal);
        if (end < 0 || text[end - 1] == ' ' || LonePipe().Match(text, lineStart) is { Success: true } lone && lone.Index < lineEnd)
        {
            return false;
        }

        var position = processor.GetSourcePosition(start, out var line, out var column);
        processor.Inline = new SpoilerInline
        {
            Text = text[(start + 2)..end],
            Span = new(position, position + end + 1 - start),
            Line = line,
            Column = column
        };

        slice.Start = end + 2;
        return true;
    }

    [GeneratedRegex(@"(?<!\|)\|(?!\|)")]
    private static partial Regex LonePipe();
}
