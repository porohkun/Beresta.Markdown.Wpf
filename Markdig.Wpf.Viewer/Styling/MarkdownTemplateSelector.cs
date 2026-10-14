namespace MarkdigWpfViewer.Styling;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ViewModels;

public sealed class MarkdownTemplateSelector : DataTemplateSelector
{
    private static readonly DependencyProperty CachedViewerProperty =
        DependencyProperty.RegisterAttached(
            "CachedViewer",
            typeof(MarkdownViewer),
            typeof(MarkdownTemplateSelector),
            new(null));

    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
    {
        var viewer = GetOrFindViewer(container);

        var provider = viewer?.StyleProvider;
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

            current = VisualTreeHelper.GetParent(current) ??
                      LogicalTreeHelper.GetParent(current);
        }

        return null;
    }
}