namespace Beresta.Markdown.Wpf.Tests;

using System.Windows;
using Extensions;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using ViewModels;

/// <summary>Расширение, собранное из делегатов.</summary>
internal sealed class LambdaExtension : MdExtension
{
    public Action<MarkdownPipelineBuilder>? Setup { get; init; }

    public Func<Block, MdConvertContext, BlockVm?>? Block { get; init; }

    public Func<Inline, MdConvertContext, InlineVm?>? Inline { get; init; }

    public Func<Inline, MdConvertContext, IReadOnlyList<InlineVm>?>? Inlines { get; init; }

    public Func<object, DataTemplate?>? Template { get; init; }

    public Func<NodeVm, object?>? State { get; init; }

    public override void SetupPipeline(MarkdownPipelineBuilder pipeline) => Setup?.Invoke(pipeline);

    public override BlockVm? ConvertBlock(Block block, MdConvertContext context) => Block?.Invoke(block, context);

    public override InlineVm? ConvertInline(Inline inline, MdConvertContext context) => Inline?.Invoke(inline, context);

    public override IReadOnlyList<InlineVm>? ConvertInlines(Inline inline, MdConvertContext context)
        => Inlines is not null ? Inlines(inline, context) : base.ConvertInlines(inline, context);

    public override DataTemplate? SelectTemplate(object item, DependencyObject container) => Template?.Invoke(item);

    public override object? CreateState(NodeVm node) => State?.Invoke(node);
}