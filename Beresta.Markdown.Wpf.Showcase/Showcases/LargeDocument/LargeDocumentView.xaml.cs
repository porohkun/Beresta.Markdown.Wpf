namespace Beresta.Markdown.Wpf.Showcase.Showcases.LargeDocument;

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Localization;
using Presenters;
using ViewModels;

/// <summary>Большой документ в общей прокрутке; сверху — сколько в нём блоков, сколько контролов создано и сколько видно.</summary>
public partial class LargeDocumentView : UserControl
{
    public LargeDocumentView()
    {
        InitializeComponent();
        Localizer.Follow(this, UpdateCounter);
    }

    private void OnViewportChanged(object? sender, EventArgs e) => UpdateCounter();

    private void UpdateCounter()
    {
        var created = Visuals.FirstDescendant<MdBlockPanel>(Viewer)?.Children
            .OfType<FrameworkElement>()
            .Where(container => container.DataContext is not BlankLineVm)
            .ToList() ?? [];
        Counter.Text = string.Format(
            CultureInfo.CurrentUICulture,
            Localizer.Text("LargeDocument.Counter"),
            Viewer.Items.Cast<object>().Count(item => item is not BlankLineVm),
            created.Count,
            created.Count(IsInView));
    }

    /// <summary>Контрол хотя бы краем попадает в видимую область прокрутки.</summary>
    private bool IsInView(FrameworkElement element)
    {
        var bounds = element.TransformToAncestor(Scroll).TransformBounds(new Rect(element.RenderSize));
        return bounds.Bottom > 0 && bounds.Top < Scroll.ViewportHeight;
    }
}
