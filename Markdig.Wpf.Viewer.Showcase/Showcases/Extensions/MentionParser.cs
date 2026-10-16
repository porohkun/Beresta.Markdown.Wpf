namespace MarkdigWpfViewer.Showcase.Showcases.Extensions;

using Markdig.Helpers;
using Markdig.Parsers;

/// <summary>Разбирает <c>@имя</c> из букв и цифр.</summary>
public sealed class MentionParser : InlineParser
{
    public MentionParser()
    {
        OpeningCharacters = ['@'];
    }

    public override bool Match(InlineProcessor processor, ref StringSlice slice)
    {
        var start = slice.Start;
        var length = 0;
        while (char.IsLetterOrDigit(slice.PeekChar(length + 1)))
        {
            length++;
        }

        if (length == 0)
        {
            return false;
        }

        var position = processor.GetSourcePosition(start, out var line, out var column);
        processor.Inline = new MentionInline
        {
            Name = slice.Text.Substring(start + 1, length),
            Span = new(position, position + length),
            Line = line,
            Column = column
        };

        slice.Start += length + 1;
        return true;
    }
}