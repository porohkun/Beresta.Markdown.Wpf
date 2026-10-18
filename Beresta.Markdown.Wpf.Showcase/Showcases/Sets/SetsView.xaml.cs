namespace Beresta.Markdown.Wpf.Showcase.Showcases.Sets;

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

/// <summary>Документ диалекта во вьюере с набором этого диалекта или без него.</summary>
public partial class SetsView : UserControl
{
    private static readonly string[] ShownLinks = ["wiki:", "tag:", "mention:", "https://example.com/"];

    public SetsView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SetsViewModel model)
        {
            model.PropertyChanged += OnModelChanged;
            Connect(model);
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SetsViewModel model)
        {
            model.PropertyChanged -= OnModelChanged;
        }
    }

    private void OnModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is SetsViewModel model && e.PropertyName is nameof(SetsViewModel.Selected) or nameof(SetsViewModel.IsSetOn))
        {
            Connect(model);
        }
    }

    private void Connect(SetsViewModel model)
    {
        Viewer.Extensions.Clear();
        if (model.IsSetOn)
        {
            Viewer.Extensions.Add(model.Selected.Set);
        }
    }

    private void OnLink(object sender, MarkdownLinkEventArgs e)
    {
        if (DataContext is SetsViewModel model && ShownLinks.Any(prefix => e.Href.StartsWith(prefix, StringComparison.Ordinal)))
        {
            model.LastHref = e.Href;
            e.Handled = true;
        }
    }
}
