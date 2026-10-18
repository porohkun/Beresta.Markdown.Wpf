namespace Beresta.Markdown.Wpf.Kit;

using Markdig.Helpers;
using Markdig.Parsers;

/// <summary>Разбор комментария <c>%%…%%</c> внутри абзаца.</summary>
internal sealed class CommentParser : InlineParser
{
    public CommentParser()
    {
        OpeningCharacters = ['%'];
    }

    public override bool Match(InlineProcessor processor, ref StringSlice slice)
    {
        var start = slice.Start;
        if (slice.PeekCharExtra(1) != '%' || start + 2 > slice.End)
        {
            return false;
        }

        var end = slice.Text.IndexOf("%%", start + 2, slice.End - (start + 2) + 1, StringComparison.Ordinal);
        if (end < 0)
        {
            return false;
        }

        var position = processor.GetSourcePosition(start, out var line, out var column);
        processor.Inline = new CommentInline
        {
            Span = new(position, position + end + 1 - start),
            Line = line,
            Column = column
        };

        slice.Start = end + 2;
        return true;
    }
}
