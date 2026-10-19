namespace Beresta.Markdown.Wpf.Extensions;

using System.Windows;
using System.Windows.Markup;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using ViewModels;

/// <summary>
/// A set of extensions as one extension: asks its extensions in order. Goes into
/// <see cref="MarkdownViewer.Extensions"/> next to single extensions; assemble it before
/// connecting.
/// </summary>
[ContentProperty(nameof(Extensions))]
public class MdExtensionSet : MdExtension
{
    /// <summary>The extensions of the set; the order is the order in which they are asked.</summary>
    public MdExtensionCollection Extensions { get; } = [];

    /// <inheritdoc/>
    public override bool AllowsBackgroundParse => Extensions.All(extension => extension.AllowsBackgroundParse);

    /// <inheritdoc/>
    public override void SetupPipeline(MarkdownPipelineBuilder pipeline)
    {
        foreach (var extension in Extensions)
        {
            extension.SetupPipeline(pipeline);
        }
    }

    /// <inheritdoc/>
    public override BlockVm? ConvertBlock(Block block, MdConvertContext context)
        => Extensions
            .Select(extension => extension.ConvertBlock(block, context))
            .FirstOrDefault(converted => converted is not null);

    /// <inheritdoc/>
    public override IReadOnlyList<BlockVm>? ConvertBlocks(Block block, MdConvertContext context)
        => Extensions
            .Select(extension => extension.ConvertBlocks(block, context))
            .FirstOrDefault(converted => converted is not null);

    /// <inheritdoc/>
    public override InlineVm? ConvertInline(Inline inline, MdConvertContext context)
        => Extensions
            .Select(extension => extension.ConvertInline(inline, context))
            .FirstOrDefault(converted => converted is not null);

    /// <inheritdoc/>
    public override IReadOnlyList<InlineVm>? ConvertInlines(Inline inline, MdConvertContext context)
        => Extensions
            .Select(extension => extension.ConvertInlines(inline, context))
            .FirstOrDefault(converted => converted is not null);

    /// <inheritdoc/>
    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => Extensions
            .Select(extension => extension.SelectTemplate(item, container))
            .FirstOrDefault(template => template is not null);

    /// <inheritdoc/>
    public override object? CreateState(NodeVm node)
        => Extensions
            .Select(extension => extension.CreateState(node))
            .FirstOrDefault(state => state is not null);
}
