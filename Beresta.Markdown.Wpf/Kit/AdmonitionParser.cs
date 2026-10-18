namespace Beresta.Markdown.Wpf.Kit;

using System.Text.RegularExpressions;
using Markdig.Parsers;
using Markdig.Syntax;

/// <summary>Разбор предупреждений MkDocs: строка <c>!!!</c>, <c>???</c> или <c>???+</c>, затем блоки с отступом в четыре пробела.</summary>
internal sealed partial class AdmonitionParser : BlockParser
{
    private const int ContentIndent = 4;

    public AdmonitionParser()
    {
        OpeningCharacters = ['!', '?'];
    }

    public override BlockState TryOpen(BlockProcessor processor)
    {
        if (processor.IsCodeIndent || Opening().Match(processor.Line.ToString()) is not { Success: true } match)
        {
            return BlockState.None;
        }

        var mark = match.Groups["mark"].Value;
        processor.NewBlocks.Push(new AdmonitionBlock(this)
        {
            Kind = match.Groups["kind"].Value,
            Title = match.Groups["title"].Success && match.Groups["title"].Length > 0 ? match.Groups["title"].Value : null,
            IsCollapsible = mark[0] == '?',
            IsExpanded = mark == "???+",
            Line = processor.LineIndex,
            Column = processor.Column,
            Span = new(processor.Start, processor.Line.End)
        });

        return BlockState.ContinueDiscard;
    }

    public override BlockState TryContinue(BlockProcessor processor, Block block)
    {
        if (processor.IsBlankLine)
        {
            return BlockState.Continue;
        }

        if (processor.Indent < ContentIndent)
        {
            return BlockState.Break;
        }

        processor.GoToColumn(processor.ColumnBeforeIndent + ContentIndent);
        block.UpdateSpanEnd(processor.Line.End);
        return BlockState.Continue;
    }

    [GeneratedRegex("""^(?<mark>!!!|\?\?\?\+?)[ \t]+(?<kind>[A-Za-z][\w-]*)(?:[ \t]+[\w-]+)*(?:[ \t]+"(?<title>[^"]*)")?[ \t]*$""")]
    private static partial Regex Opening();
}
