namespace MarkdigWpfViewer.Showcase.Showcases.Kit;

using System.Windows.Controls;

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
}
