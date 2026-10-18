namespace Beresta.Markdown.Wpf.Kit;

using Markdig.Parsers;
using Markdig.Syntax;

/// <summary>
/// Разбор комментария блоком: строка начинается с <c>%%</c>, закрывающих <c>%%</c> в ней нет; комментарий
/// идёт до строки с <c>%%</c> включительно. Строка с обоими <c>%%</c> — комментарий внутри абзаца.
/// </summary>
internal sealed class CommentBlockParser : BlockParser
{
    public CommentBlockParser()
    {
        OpeningCharacters = ['%'];
    }

    public override BlockState TryOpen(BlockProcessor processor)
    {
        var line = processor.Line.ToString();
        if (processor.IsCodeIndent || !line.StartsWith("%%", StringComparison.Ordinal) || line.IndexOf("%%", 2, StringComparison.Ordinal) >= 0)
        {
            return BlockState.None;
        }

        processor.NewBlocks.Push(new CommentBlock(this)
        {
            Line = processor.LineIndex,
            Column = processor.Column,
            Span = new(processor.Start, processor.Line.End)
        });

        return BlockState.ContinueDiscard;
    }

    public override BlockState TryContinue(BlockProcessor processor, Block block)
    {
        block.UpdateSpanEnd(processor.Line.End);
        return processor.Line.ToString().Contains("%%", StringComparison.Ordinal) ? BlockState.BreakDiscard : BlockState.ContinueDiscard;
    }
}
