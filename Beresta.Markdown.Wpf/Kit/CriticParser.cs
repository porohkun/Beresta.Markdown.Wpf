namespace Beresta.Markdown.Wpf.Kit;

using Markdig.Helpers;
using Markdig.Parsers;

/// <summary>Разбор CriticMarkup: <c>{++…++}</c>, <c>{--…--}</c>, <c>{~~…~&gt;…~~}</c>, <c>{==…==}</c>, <c>{&gt;&gt;…&lt;&lt;}</c>.</summary>
internal sealed class CriticParser : InlineParser
{
    private const int MarkLength = 3;

    private static readonly (string Open, string Close, CriticKind Kind)[] Marks =
    [
        ("{++", "++}", CriticKind.Insertion),
        ("{--", "--}", CriticKind.Deletion),
        ("{~~", "~~}", CriticKind.Substitution),
        ("{==", "==}", CriticKind.Highlight),
        ("{>>", "<<}", CriticKind.Comment)
    ];

    public CriticParser()
    {
        OpeningCharacters = ['{'];
    }

    public override bool Match(InlineProcessor processor, ref StringSlice slice)
    {
        var start = slice.Start;
        var text = slice.Text;
        if (start + MarkLength > slice.End)
        {
            return false;
        }

        foreach (var (open, close, kind) in Marks)
        {
            if (string.CompareOrdinal(text, start, open, 0, MarkLength) != 0)
            {
                continue;
            }

            var end = text.IndexOf(close, start + MarkLength, slice.End - (start + MarkLength) + 1, StringComparison.Ordinal);
            if (end < 0)
            {
                return false;
            }

            var content = text[(start + MarkLength)..end].Replace("\r\n", "\n");
            string? replacement = null;
            if (kind == CriticKind.Substitution)
            {
                var arrow = content.IndexOf("~>", StringComparison.Ordinal);
                if (arrow < 0)
                {
                    return false;
                }

                replacement = content[(arrow + 2)..];
                content = content[..arrow];
            }

            var position = processor.GetSourcePosition(start, out var line, out var column);
            processor.Inline = new CriticInline
            {
                Kind = kind,
                Text = content,
                Replacement = replacement,
                Span = new(position, position + end + MarkLength - start - 1),
                Line = line,
                Column = column
            };

            slice.Start = end + MarkLength;
            return true;
        }

        return false;
    }
}
