namespace Beresta.Markdown.Wpf.Styling;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Presenters;
using ViewModels;

/// <summary>
/// The template for an element of the document: first from the extensions of the viewer, then from
/// its style provider.
/// </summary>
public sealed class MarkdownTemplateSelector : DataTemplateSelector
{
    private static readonly DependencyProperty CachedViewerProperty =
        DependencyProperty.RegisterAttached(
            "CachedViewer",
            typeof(MarkdownViewer),
            typeof(MarkdownTemplateSelector),
            new(null));

    /// <inheritdoc/>
    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
    {
        if (item is null)
            return null;

        if (item is SourceWindowVm)
            return SourceWindowHost.Template;

        var viewer = GetOrFindViewer(container);
        if (viewer is null)
            return null;

        foreach (var extension in viewer.ActiveExtensions)
        {
            if (viewer.Guard.Call(extension, () => extension.SelectTemplate(item, container), null) is { } template)
                return template;
        }

        var provider = viewer.StyleProvider;
        if (provider is null)
            return null;

        return item switch
        {
            ParagraphVm => provider.ParagraphTemplate,
            HeadingVm => provider.HeadingTemplate,
            CodeBlockVm => provider.CodeBlockTemplate,
            QuoteVm => provider.QuoteTemplate,
            ListVm => provider.ListTemplate,
            TableVm => provider.TableTemplate,
            ThematicBreakVm => provider.HorizontalRuleTemplate,
            BlankLineVm => provider.BlankLineTemplate,
            FootnoteVm => provider.FootnoteTemplate,
            ImageVm => provider.ImageTemplate,
            TaskVm => provider.TaskTemplate,
            FootnoteLinkVm => provider.FootnoteLinkTemplate,
            FigureVm => provider.FigureTemplate,
            FigureCaptionVm => provider.FigureCaptionTemplate,
            FooterVm => provider.FooterTemplate,
            CalloutVm => provider.CalloutTemplate,
            DetailsVm => provider.DetailsTemplate,
            DefinitionListVm => provider.DefinitionListTemplate,
            AlignedVm => provider.AlignedTemplate,
            _ => null
        };
    }

    private static MarkdownViewer? GetOrFindViewer(DependencyObject container)
    {
        if (container.GetValue(CachedViewerProperty) is MarkdownViewer cached)
            return cached;

        var current = container;

        while (current is not null)
        {
            if (current is MarkdownViewer viewer)
            {
                container.SetValue(CachedViewerProperty, viewer);
                return viewer;
            }

            current = current is Visual or Visual3D
                ? VisualTreeHelper.GetParent(current) ?? LogicalTreeHelper.GetParent(current)
                : LogicalTreeHelper.GetParent(current);
        }

        return null;
    }
}