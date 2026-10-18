namespace Beresta.Markdown.Wpf.Kit;

using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Syntax;

/// <summary>Разбор статуса задачи <c>[c]</c> с пробелом после — первым в абзаце пункта списка.</summary>
internal sealed class TaskStatusParser : InlineParser
{
    private const string Statuses = "/->?!";

    public TaskStatusParser()
    {
        OpeningCharacters = ['['];
    }

    public override bool Match(InlineProcessor processor, ref StringSlice slice)
    {
        var status = slice.PeekCharExtra(1);
        if (processor.Block?.Parent is not ListItemBlock
            || processor.Inline is not null
            || !Statuses.Contains(status)
            || slice.PeekCharExtra(2) != ']'
            || slice.PeekCharExtra(3) is not (' ' or '\t'))
        {
            return false;
        }

        var position = processor.GetSourcePosition(slice.Start, out var line, out var column);
        processor.Inline = new TaskStatusInline
        {
            Status = status,
            Span = new(position, position + 2),
            Line = line,
            Column = column
        };

        slice.Start += 3;
        return true;
    }
}
