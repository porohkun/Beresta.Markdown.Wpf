namespace MarkdigWpfViewer.Extensions;

using System.Windows;
using System.Windows.Markup;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using ViewModels;

/// <summary>
/// Набор расширений одним расширением: опрашивает свои расширения по порядку. Подключается в
/// <see cref="MarkdownViewer.Extensions"/> вперемешку с одиночными; собирается до подключения.
/// </summary>
[ContentProperty(nameof(Extensions))]
public class MdExtensionSet : MdExtension
{
    /// <summary>Расширения набора; порядок — порядок опроса.</summary>
    public MdExtensionCollection Extensions { get; } = [];

    public override void SetupPipeline(MarkdownPipelineBuilder pipeline)
    {
        foreach (var extension in Extensions)
        {
            extension.SetupPipeline(pipeline);
        }
    }

    public override BlockVm? ConvertBlock(Block block, MdConvertContext context)
        => Extensions
            .Select(extension => extension.ConvertBlock(block, context))
            .FirstOrDefault(converted => converted is not null);

    public override IReadOnlyList<BlockVm>? ConvertBlocks(Block block, MdConvertContext context)
        => Extensions
            .Select(extension => extension.ConvertBlocks(block, context))
            .FirstOrDefault(converted => converted is not null);

    public override InlineVm? ConvertInline(Inline inline, MdConvertContext context)
        => Extensions
            .Select(extension => extension.ConvertInline(inline, context))
            .FirstOrDefault(converted => converted is not null);

    public override IReadOnlyList<InlineVm>? ConvertInlines(Inline inline, MdConvertContext context)
        => Extensions
            .Select(extension => extension.ConvertInlines(inline, context))
            .FirstOrDefault(converted => converted is not null);

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => Extensions
            .Select(extension => extension.SelectTemplate(item, container))
            .FirstOrDefault(template => template is not null);

    public override object? CreateState(NodeVm node)
        => Extensions
            .Select(extension => extension.CreateState(node))
            .FirstOrDefault(state => state is not null);
}
