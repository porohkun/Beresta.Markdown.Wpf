namespace Beresta.Markdown.Wpf.Kit;

using Extensions;
using Markdig;
using Markdig.Syntax.Inlines;
using ViewModels;

/// <summary>
/// Spoilers <c>||text||</c> as styled text <c>Spoiler</c>, painted over until clicked; whether it
/// is revealed is <see cref="SpoilerState"/>. A spoiler is not parsed in a table row.
/// </summary>
public sealed class Spoilers : MdExtension
{
    /// <inheritdoc/>
    public override void SetupPipeline(MarkdownPipelineBuilder pipeline)
        => pipeline.Extensions.AddIfNotAlready<SpoilerMarkdigExtension>();

    /// <inheritdoc/>
    public override IReadOnlyList<InlineVm>? ConvertInlines(Inline inline, MdConvertContext context)
        => inline is SpoilerInline spoiler
            ? [context.CreateRun(spoiler.Text) with { StyleName = "Spoiler", Markup = new("||", "||") }]
            : null;

    /// <inheritdoc/>
    public override object? CreateState(NodeVm node)
        => node is RunVm { StyleName: "Spoiler" } ? new SpoilerState() : null;
}
