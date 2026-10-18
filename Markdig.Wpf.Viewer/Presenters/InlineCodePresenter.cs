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

    /// <summary>Линии по тексту от начертаний вокруг: зачёркивание, подчёркивание вставки.</summary>
    public static readonly DependencyProperty TextDecorationsProperty =
        DependencyProperty.Register(
            nameof(TextDecorations),
            typeof(TextDecorationCollection),
            typeof(InlineCodePresenter));

    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public TextDecorationCollection? TextDecorations
    {
        get => (TextDecorationCollection?)GetValue(TextDecorationsProperty);
        set => SetValue(TextDecorationsProperty, value);
    }
}