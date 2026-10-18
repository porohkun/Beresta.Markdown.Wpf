namespace Beresta.Markdown.Wpf.Kit;

using Markdig.Helpers;
using Markdig.Parsers;

/// <summary>
/// Разбор <c>@имя</c>: буквы, цифры, <c>_</c>, <c>-</c>, <c>.</c> и <c>/</c> команды. Не упоминание —
/// <c>@</c> сразу после буквы или цифры, как в адресе почты.
/// </summary>
internal sealed class MentionParser : InlineParser
{
    public MentionParser()
    {
        OpeningCharacters = ['@'];
    }

    public override bool Match(InlineProcessor processor, ref StringSlice slice)
    {
        var previous = slice.PeekCharExtra(-1);
        if (char.IsLetterOrDigit(previous) || previous is '_' or '.' or '-' or '@')
        {
            return false;
        }

        var start = slice.Start;
        var length = 0;
        while (slice.PeekCharExtra(length + 1) is var symbol && (char.IsLetterOrDigit(symbol) || symbol is '_' or '-' or '.' or '/'))
        {
            length++;
        }

        while (length > 0 && slice.Text[start + length] is '-' or '.' or '/')
        {
            length--;
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
