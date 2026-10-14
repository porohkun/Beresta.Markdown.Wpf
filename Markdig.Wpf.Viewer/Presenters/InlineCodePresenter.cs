namespace MarkdigWpfViewer.Presenters;

using System.Windows;
using System.Windows.Controls;

public class InlineCodePresenter : Control
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(InlineCodePresenter));

    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}