namespace MarkdigWpfViewer.Kit;

using System.Text.RegularExpressions;
using Markdig.Helpers;
using Markdig.Parsers;

/// <summary>Разбор шорткодов Jekyll <c>{%имя аргументы%}</c> и Hugo <c>{{&lt; имя аргументы &gt;}}</c>, <c>{{% … %}}</c> в пределах строки.</summary>
internal sealed partial class ShortcodeParser : InlineParser
{
    public ShortcodeParser()
    {
        OpeningCharacters = ['{'];
    }

    public override bool Match(InlineProcessor processor, ref StringSlice slice)
    {
        var start = slice.Start;
        var text = slice.Text;
        var lineEnd = text.IndexOf('\n', start) is var newline and >= 0 ? newline : slice.End + 1;
        if (Shortcode().Match(text[start..Math.Min(lineEnd, slice.End + 1)]) is not { Success: true } match)
        {
            return false;
        }

        var arguments = new List<string>();
        var named = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match argument in Argument().Matches(match.Groups["arguments"].Value))
        {
            if (argument.Groups["key"].Success)
            {
                named[argument.Groups["key"].Value] = argument.Groups["value"].Value;
            }
            else
            {
                arguments.Add(argument.Groups["value"].Value);
            }
        }

        var position = processor.GetSourcePosition(start, out var line, out var column);
        processor.Inline = new ShortcodeInline
        {
            Shortcode = new(match.Groups["name"].Value, arguments, named),
            Span = new(position, position + match.Length - 1),
            Line = line,
            Column = column
        };

        slice.Start = start + match.Length;
        return true;
    }

    [GeneratedRegex(@"^(?:\{%-?|\{\{[<%])\s*(?<name>[A-Za-z][\w-]*)(?<arguments>(?:\s+[^%>{}]*?)?)\s*(?:-?%\}|[>%]\}\})")]
    private static partial Regex Shortcode();

    [GeneratedRegex("""(?:(?<key>[A-Za-z][\w-]*)=)?(?:"(?<value>[^"]*)"|'(?<value>[^']*)'|(?<value>[^\s"']+))""")]
    private static partial Regex Argument();
}
