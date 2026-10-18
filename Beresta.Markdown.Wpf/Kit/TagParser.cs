namespace Beresta.Markdown.Wpf.Kit;

using Markdig.Helpers;
using Markdig.Parsers;

/// <summary>
/// Разбор <c>#тег</c>: буквы, цифры, <c>_</c>, <c>-</c> и <c>/</c>. Не тег — одни цифры (это задача) и
/// решётка сразу после буквы или цифры, как в <c>C#</c>.
/// </summary>
internal sealed class TagParser : InlineParser
{
    public TagParser()
    {
        OpeningCharacters = ['#'];
    }

    public override bool Match(InlineProcessor processor, ref StringSlice slice)
    {
        var previous = slice.PeekCharExtra(-1);
        if (char.IsLetterOrDigit(previous) || previous is '_' or '/' or '#' or '&')
        {
            return false;
        }

        var start = slice.Start;
        var length = 0;
        while (slice.PeekCharExtra(length + 1) is var symbol && (char.IsLetterOrDigit(symbol) || symbol is '_' or '-' or '/'))
        {
            length++;
        }

        while (length > 0 && slice.Text[start + length] is '-' or '/')
        {
            length--;
        }

        var name = slice.Text.Substring(start + 1, length);
        if (length == 0 || name.All(char.IsDigit))
        {
            return false;
        }

        var position = processor.GetSourcePosition(start, out var line, out var column);
        processor.Inline = new TagInline
        {
            Name = name,
            Span = new(position, position + length),
            Line = line,
            Column = column
        };

        slice.Start += length + 1;
        return true;
    }
}
