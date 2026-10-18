namespace Beresta.Markdown.Wpf.Showcase.Showcases.Preview;

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Scrolling;

/// <summary>Редактор и вьюер в синхронной прокрутке; у редактора номера строк исходника.</summary>
public partial class PreviewView : UserControl
{
    private MdTextBoxLines? _lines;
    private MdScrollSync? _sync;

    public PreviewView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_sync is not null)
        {
            return;
        }

        _lines = new(Editor);
        _sync = new(_lines, Viewer, MdLineMap.Identity);
        DrawNumbers();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _sync?.Dispose();
        _sync = null;
        _lines = null;
    }

    private void OnEditorChanged(object sender, RoutedEventArgs e) => DrawNumbers();

    private void OnNumbersVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) => DrawNumbers();

    /// <summary>Номера видимых строк исходника — у экранной строки, с которой каждая начинается.</summary>
    private void DrawNumbers()
    {
        Numbers.Children.Clear();
        if (_lines is null || !Numbers.IsVisible)
        {
            return;
        }

        foreach (var (line, top, height) in _lines.VisibleRows(Numbers))
        {
            var number = new TextBlock
            {
                Text = (line + 1).ToString(CultureInfo.InvariantCulture),
                Width = Numbers.ActualWidth,
                TextAlignment = TextAlignment.Right,
                FontSize = 12
            };
            number.SetResourceReference(TextBlock.ForegroundProperty, "Md.Gutter.Foreground");
            number.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetTop(number, top + (height - number.DesiredSize.Height) / 2);
            Numbers.Children.Add(number);
        }
    }
}
