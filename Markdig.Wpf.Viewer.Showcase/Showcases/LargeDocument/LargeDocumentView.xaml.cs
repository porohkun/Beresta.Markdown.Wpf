namespace MarkdigWpfViewer.Showcase.Showcases.LargeDocument;

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Presenters;
using ViewModels;

/// <summary>Большой документ в общей прокрутке; сверху — сколько в нём блоков, сколько контролов создано и сколько видно.</summary>
public partial class LargeDocumentView : UserControl
{
    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

    public LargeDocumentView()
    {
        InitializeComponent();
    }

    private void OnViewportChanged(object? sender, EventArgs e)
    {
        var created = Visuals.FirstDescendant<MdBlockPanel>(Viewer)?.Children
            .OfType<FrameworkElement>()
            .Where(container => container.DataContext is not BlankLineVm)
            .ToList() ?? [];
        Counter.Text = string.Format(
            Russian,
            "Блоков в документе: {0:N0}. Контролов создано: {1:N0}, из них видно: {2:N0}.",
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
