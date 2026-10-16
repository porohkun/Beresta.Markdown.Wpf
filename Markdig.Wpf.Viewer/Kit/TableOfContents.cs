namespace MarkdigWpfViewer.Kit;

using System.Windows;
using Extensions;
using Markdig.Syntax;
using ViewModels;

/// <summary>
/// Оглавление на месте абзаца ровно из <c>[TOC]</c>, <c>[[_TOC_]]</c> или <c>[[toc]]</c>: заголовки
/// документа, пункт ведёт к заголовку. Вид — шаблон темы <c>Md.Toc.Template</c>.
/// </summary>
public sealed class TableOfContents : MdExtension
{
    private static readonly string[] Markers = ["[TOC]", "[[_TOC_]]", "[[toc]]"];

    public override BlockVm? ConvertBlock(Block block, MdConvertContext context)
        => block is ParagraphBlock paragraph && Markers.Contains(context.GetSource(paragraph).Trim(), StringComparer.OrdinalIgnoreCase)
            ? new TocVm()
            : null;

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item is TocVm && container is FrameworkElement element ? element.TryFindResource("Md.Toc.Template") as DataTemplate : null;
}
