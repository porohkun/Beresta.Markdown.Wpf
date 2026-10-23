namespace Beresta.Markdown.Wpf.Presenters;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Kit;

/// <summary>
/// The input field of a value in the properties card: Enter and losing focus write the value into the
/// source, Esc returns the written one. Its <see cref="IsActive"/> tells the template when to show it.
/// </summary>
public sealed class MdPropertyBox : TextBox
{
    private static readonly DependencyProperty ControlsEditableProperty =
        MarkdownViewer.AreControlsEditableProperty.AddOwner(
            typeof(MdPropertyBox),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits, OnEditableChanged));

    private static readonly DependencyProperty FrontMatterEditableProperty =
        MarkdownViewer.IsFrontMatterEditableProperty.AddOwner(
            typeof(MdPropertyBox),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits, OnEditableChanged));

    private static readonly DependencyPropertyKey IsActivePropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(IsActive),
            typeof(bool),
            typeof(MdPropertyBox),
            new(false));

    /// <summary>
    /// The field edits its value: controls edit the source, front matter is editable, and the value
    /// can be written anew.
    /// </summary>
    public static readonly DependencyProperty IsActiveProperty = IsActivePropertyKey.DependencyProperty;

    /// <summary>Creates a field; its value is the <see cref="PropertyVm"/> in the data context.</summary>
    public MdPropertyBox()
    {
        DataContextChanged += (_, _) =>
        {
            Revert();
            Update();
        };
    }

    /// <inheritdoc cref="IsActiveProperty"/>
    public bool IsActive => (bool)GetValue(IsActiveProperty);

    /// <summary>Пишет набранное в исходник, если оно отличается от записанного.</summary>
    internal void Commit()
    {
        if (!IsActive || DataContext is not PropertyVm property || Text == property.ValueText)
        {
            return;
        }

        for (DependencyObject? node = this; node is not null; node = VisualTreeHelper.GetParent(node))
        {
            if (node is FrameworkElement { DataContext: PropertiesVm card })
            {
                MarkdownViewer.Owning(this)?.RequestSourceEdit(card, new PropertyEdit(property, Text));
                return;
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Commit();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            Revert();
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    /// <inheritdoc/>
    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnLostKeyboardFocus(e);
        Commit();
    }

    private static void OnEditableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MdPropertyBox)d).Update();

    private void Revert() => Text = (DataContext as PropertyVm)?.ValueText ?? string.Empty;

    private void Update()
        => SetValue(
            IsActivePropertyKey,
            (bool)GetValue(ControlsEditableProperty)
            && (bool)GetValue(FrontMatterEditableProperty)
            && DataContext is PropertyVm { ValueKind: not MdValueKind.ReadOnly });
}
