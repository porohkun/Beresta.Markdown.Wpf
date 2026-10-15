namespace MarkdigWpfViewer.Tests;

using Abstractions;
using Markdig;
using ViewModels;

/// <summary>
/// Модель документа одной строкой: <c>P(текст) _ L[1. P(а); 2. P(б)]</c> — абзац, отступ и список.
/// </summary>
internal static class ModelDump
{
    public static string Of(string markdown, MarkdownPipeline? pipeline = null)
        => Of(ModelBuilder.Parse(markdown, pipeline));

    public static string Of(IEnumerable<IMdBlockVm> blocks)
        => string.Join(" ", blocks.Select(Block));

    public static string Text(IEnumerable<InlineVm> inlines)
        => string.Concat(inlines.Select(inline => inline.LineBreak ? "\n" : inline.Text));

    private static string Block(IMdBlockVm block) => block switch
    {
        ParagraphVm paragraph => $"P({Text(paragraph.Inlines)})",
        HeadingVm heading => $"H{heading.Level}({Text(heading.Inlines)})",
        BlankLineVm => "_",
        ThematicBreakVm => "---",
        CodeBlockVm code => $"C({code.Code})",
        QuoteVm quote => $"Q[{Of(quote.Blocks)}]",
        ListVm list => $"L[{string.Join("; ", list.Items.Select(item => $"{item.MarkerText} {Of(item.Blocks)}"))}]",
        TableVm table => $"T({table.Rows.Count}x{table.ColumnCount})",
        _ => block.GetType().Name
    };
}
