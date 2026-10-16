namespace MarkdigWpfViewer.Showcase.Showcases.Kit;

using System.Windows.Controls;
using MarkdigWpfViewer.Kit;

/// <summary>Документ в синтаксисе других программ с готовыми расширениями.</summary>
public partial class KitView : UserControl
{
    private static readonly string[] OwnSchemes = ["wiki:", "tag:", "mention:"];

    public KitView()
    {
        InitializeComponent();
    }

    private void OnLink(object sender, MarkdownLinkEventArgs e)
    {
        if (DataContext is KitViewModel model && OwnSchemes.Any(scheme => e.Href.StartsWith(scheme, StringComparison.Ordinal)))
        {
            model.LastLink = "Нажата ссылка: " + e.Href;
            e.Handled = true;
        }
    }

    private void OnDecided(object sender, MdCriticDecidedEventArgs e)
        => (DataContext as KitViewModel)?.Replace(e.Change.Start, e.Change.Length, e.Replacement);

    private void OnTaskToggled(object sender, MdTaskToggledEventArgs e)
        => (DataContext as KitViewModel)?.Replace(e.Task.Source.Start, e.Task.Source.Length, e.IsChecked ? "[x]" : "[ ]");
}
