namespace MarkdigWpfViewer.Showcase.Showcases.Kit;

using System.Windows.Controls;
using MarkdigWpfViewer.Kit;
using MarkdigWpfViewer.ViewModels;

/// <summary>Документ в синтаксисе других программ с готовыми расширениями.</summary>
public partial class KitView : UserControl
{
    private static readonly string[] ShownLinks = ["wiki:", "tag:", "mention:", "https://example.com/"];

    public KitView()
    {
        InitializeComponent();
        ShortcodeHandlers.Handlers["kbd"] = shortcode => new RunVm(string.Join(" ", shortcode.Arguments), Kbd: true);
    }

    private void OnLink(object sender, MarkdownLinkEventArgs e)
    {
        if (DataContext is KitViewModel model && ShownLinks.Any(prefix => e.Href.StartsWith(prefix, StringComparison.Ordinal)))
        {
            model.LastHref = e.Href;
            e.Handled = true;
        }
    }
}
