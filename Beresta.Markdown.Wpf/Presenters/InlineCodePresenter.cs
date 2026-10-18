namespace Beresta.Markdown.Wpf.Presenters;

using System.Windows;
using System.Windows.Controls;

/// <summary>Code or a key in the middle of a line: an indivisible piece of text in a frame.</summary>
public class InlineCodePresenter : Control
{
    /// <summary>The text of the code or the key.</summary>
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(InlineCodePresenter));

    /// <summary>
    /// Lines over the text from the formatting around it: strikethrough, the underline of an
    /// insertion.
    /// </summary>
    public static readonly DependencyProperty TextDecorationsProperty =
        DependencyProperty.Register(
            nameof(TextDecorations),
            typeof(TextDecorationCollection),
            typeof(InlineCodePresenter));

    /// <inheritdoc cref="TextProperty"/>
    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <inheritdoc cref="TextDecorationsProperty"/>
    public TextDecorationCollection? TextDecorations
    {
        get => (TextDecorationCollection?)GetValue(TextDecorationsProperty);
        set => SetValue(TextDecorationsProperty, value);
    }
}