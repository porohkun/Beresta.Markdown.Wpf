namespace MarkdigWpfViewer.Kit;

using System.Windows;
using Extensions;
using Markdig.Extensions.Footnotes;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using ViewModels;

/// <summary>
/// Сноски подсказкой: ссылка на сноску показывает её текст во всплывающей подсказке, определение
/// сноски не рисуется. Вид — шаблон темы <c>Md.FootnoteTip.Template</c>.
/// </summary>
public sealed class FootnoteTips : MdExtension
{
    public override InlineVm? ConvertInline(Inline inline, MdConvertContext context)
        => inline is FootnoteLink { IsBackLink: false } link
            ? new FootnoteTipVm(link.Footnote.Order, string.Join("\n\n", link.Footnote.Select(context.GetSource)))
            : null;

    public override IReadOnlyList<BlockVm>? ConvertBlocks(Block block, MdConvertContext context)
        => block is Footnote ? [] : null;

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item is FootnoteTipVm && container is FrameworkElement element ? element.TryFindResource("Md.FootnoteTip.Template") as DataTemplate : null;
}
