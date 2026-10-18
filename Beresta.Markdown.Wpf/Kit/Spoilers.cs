namespace Beresta.Markdown.Wpf.Kit;

using Extensions;
using Markdig;
using Markdig.Syntax.Inlines;
using ViewModels;

/// <summary>
/// Спойлеры <c>||текст||</c> — оформленным текстом <c>Spoiler</c>, закрашенным, пока по нему не
/// щёлкнут; раскрытость — <see cref="SpoilerState"/>. В строке таблицы спойлер не разбирается.
/// </summary>
public sealed class Spoilers : MdExtension
{
    public override void SetupPipeline(MarkdownPipelineBuilder pipeline)
        => pipeline.Extensions.AddIfNotAlready<SpoilerMarkdigExtension>();

    public override IReadOnlyList<InlineVm>? ConvertInlines(Inline inline, MdConvertContext context)
        => inline is SpoilerInline spoiler
            ? [context.CreateRun(spoiler.Text) with { StyleName = "Spoiler", Markup = new("||", "||") }]
            : null;

    public override object? CreateState(NodeVm node)
        => node is RunVm { StyleName: "Spoiler" } ? new SpoilerState() : null;
}
