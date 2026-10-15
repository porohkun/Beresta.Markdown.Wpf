namespace MarkdigWpfViewer.Tests;

using Extensions;
using Markdig;
using ViewModels;

/// <summary>
/// Модель документа одной строкой: <c>P(текст) _ L[1. P(а); 2. P(б)]</c> — абзац, отступ и список.
/// Виджет без своего вида выводится именем типа в фигурных скобках.
/// </summary>
internal static class ModelDump
{
    public static string Of(string markdown, MarkdownPipeline? pipeline = null)
        => Of(ModelBuilder.Parse(markdown, pipeline));

    public static string Of(string markdown, params MdExtension[] extensions)
        => Of(ModelBuilder.Parse(markdown, Pipeline(extensions), extensions));

    public static string Of(IEnumerable<BlockVm> blocks)
        => string.Join(" ", blocks.Select(Block));

    public static string Text(IEnumerable<InlineVm> inlines)
        => string.Concat(inlines.Select(inline => inline switch
        {
            RunVm run => run.Text,
            LineBreakVm => "\n",
            _ => $"{{{inline.GetType().Name}}}"
        }));

    public static MarkdownPipeline Pipeline(params MdExtension[] extensions)
    {
        var builder = new MarkdownPipelineBuilder().UseAdvancedExtensions();
        foreach (var extension in extensions)
        {
            extension.SetupPipeline(builder);
        }

        return builder.Build();
    }

    private static string Block(BlockVm block) => block switch
    {
        ParagraphVm paragraph => $"P({Text(paragraph.Inlines)})",
        HeadingVm heading => $"H{heading.Level}({Text(heading.Inlines)})",
        BlankLineVm => "_",
        ThematicBreakVm => "---",
        CodeBlockVm code => $"C({code.Code})",
        QuoteVm quote => $"Q[{Of(quote.Blocks)}]",
        ListVm list => $"L[{string.Join("; ", list.Items.Select(item => $"{item.MarkerText} {Of(item.Blocks)}"))}]",
        TableVm table => $"T({table.Rows.Count}x{table.ColumnCount})",
        _ => $"{{{block.GetType().Name}}}"
    };
}