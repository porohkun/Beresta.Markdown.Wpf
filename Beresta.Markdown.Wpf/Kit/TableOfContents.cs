namespace Beresta.Markdown.Wpf.Kit;

using System.Windows;
using Extensions;
using Markdig.Syntax;
using ViewModels;

/// <summary>
/// A table of contents in place of a paragraph of exactly <c>[TOC]</c>, <c>[[_TOC_]]</c> or
/// <c>[[toc]]</c>: the headings of the document, an item leads to its heading. The look is the
/// theme template <c>Md.Toc.Template</c>.
/// </summary>
public sealed class TableOfContents : MdExtension
{
    private static readonly string[] Markers = ["[TOC]", "[[_TOC_]]", "[[toc]]"];

    /// <inheritdoc/>
    public override BlockVm? ConvertBlock(Block block, MdConvertContext context)
        => block is ParagraphBlock paragraph && Markers.Contains(context.GetSource(paragraph).Trim(), StringComparer.OrdinalIgnoreCase)
            ? new TocVm()
            : null;

    /// <inheritdoc/>
    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item is TocVm && container is FrameworkElement element ? element.TryFindResource("Md.Toc.Template") as DataTemplate : null;
}
