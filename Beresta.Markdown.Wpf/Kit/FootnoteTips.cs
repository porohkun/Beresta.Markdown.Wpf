namespace Beresta.Markdown.Wpf.Kit;

using System.Windows;
using Extensions;
using Markdig.Extensions.Footnotes;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using ViewModels;

/// <summary>
/// Footnotes as tooltips: a link to a footnote shows its text in a tooltip, the footnote definition
/// is not drawn. The look is the theme template <c>Md.FootnoteTip.Template</c>.
/// </summary>
public sealed class FootnoteTips : MdExtension
{
    /// <inheritdoc/>
    public override InlineVm? ConvertInline(Inline inline, MdConvertContext context)
        => inline is FootnoteLink { IsBackLink: false } link
            ? new FootnoteTipVm(link.Footnote.Order, string.Join("\n\n", link.Footnote.Select(context.GetSource)))
            : null;

    /// <inheritdoc/>
    public override IReadOnlyList<BlockVm>? ConvertBlocks(Block block, MdConvertContext context)
        => block is Footnote ? [] : null;

    /// <inheritdoc/>
    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item is FootnoteTipVm && container is FrameworkElement element ? element.TryFindResource("Md.FootnoteTip.Template") as DataTemplate : null;
}
