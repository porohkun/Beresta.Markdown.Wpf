namespace Beresta.Markdown.Wpf.Presenters;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Kit;

/// <summary>
/// The rows of the properties card and its row that adds a property. Its commands and the flags in its
/// rows write into the source while <see cref="IsEditing"/> is on.
/// </summary>
public sealed class MdPropertiesPresenter : ItemsControl
{
    private static readonly DependencyProperty ControlsEditableProperty =
        MarkdownViewer.AreControlsEditableProperty.AddOwner(
            typeof(MdPropertiesPresenter),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits, OnEditableChanged));

    private static readonly DependencyProperty FrontMatterEditableProperty =
        MarkdownViewer.IsFrontMatterEditableProperty.AddOwner(
            typeof(MdPropertiesPresenter),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits, OnEditableChanged));

    private static readonly DependencyPropertyKey IsEditingPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(IsEditing),
            typeof(bool),
            typeof(MdPropertiesPresenter),
            new(false));

    /// <summary>The card edits the source: controls edit the source, and front matter is editable.</summary>
    public static readonly DependencyProperty IsEditingProperty = IsEditingPropertyKey.DependencyProperty;

    static MdPropertiesPresenter()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(MdPropertiesPresenter), new FrameworkPropertyMetadata(typeof(MdPropertiesPresenter)));
        Bind(SetValueCommand, (presenter, e) => presenter.Request(e.OriginalSource, property => e.Parameter is string text && property.Place is not null ? new PropertyEdit(property, text) : null));
        Bind(AddItemCommand, (presenter, e) => presenter.Request(e.OriginalSource, property => AddItem(property, e.Parameter as string), clear: true));
        Bind(RemoveItemCommand, (presenter, e) => presenter.Request(e.OriginalSource, property => RemoveItem(property, e.Parameter as string)));
        Bind(RemoveCommand, (presenter, e) => presenter.Request(e.OriginalSource, property => new PropertyRemoval(property)));
        Bind(AddCommand, (presenter, e) => presenter.Add(e.Parameter as string));
        EventManager.RegisterClassHandler(typeof(MdPropertiesPresenter), ToggleButton.CheckedEvent, new RoutedEventHandler(OnToggled));
        EventManager.RegisterClassHandler(typeof(MdPropertiesPresenter), ToggleButton.UncheckedEvent, new RoutedEventHandler(OnToggled));
    }

    /// <summary>Writes the text in the parameter as the value of the row the command comes from.</summary>
    public static RoutedCommand SetValueCommand { get; } = new(nameof(SetValueCommand), typeof(MdPropertiesPresenter));

    /// <summary>Adds the text in the parameter as an item to the list of the row the command comes from.</summary>
    public static RoutedCommand AddItemCommand { get; } = new(nameof(AddItemCommand), typeof(MdPropertiesPresenter));

    /// <summary>Removes the item in the parameter from the list of the row the command comes from.</summary>
    public static RoutedCommand RemoveItemCommand { get; } = new(nameof(RemoveItemCommand), typeof(MdPropertiesPresenter));

    /// <summary>Removes the property of the row the command comes from, all its lines.</summary>
    public static RoutedCommand RemoveCommand { get; } = new(nameof(RemoveCommand), typeof(MdPropertiesPresenter));

    /// <summary>Adds a property with the key in the parameter before the closing line of front matter.</summary>
    public static RoutedCommand AddCommand { get; } = new(nameof(AddCommand), typeof(MdPropertiesPresenter));

    /// <inheritdoc cref="IsEditingProperty"/>
    public bool IsEditing => (bool)GetValue(IsEditingProperty);

    private static void Bind(RoutedCommand command, Action<MdPropertiesPresenter, ExecutedRoutedEventArgs> execute)
        => CommandManager.RegisterClassCommandBinding(
            typeof(MdPropertiesPresenter),
            new(
                command,
                (sender, e) =>
                {
                    execute((MdPropertiesPresenter)sender, e);
                    e.Handled = true;
                },
                (sender, e) => e.CanExecute = ((MdPropertiesPresenter)sender).IsEditing));

    private static PropertyEdit? AddItem(PropertyVm property, string? item)
        => property.Place is not null && item?.Trim() is { Length: > 0 } text
            ? new(property, string.Join(", ", [.. property.Items, text]))
            : null;

    private static PropertyEdit? RemoveItem(PropertyVm property, string? item)
    {
        var index = item is null ? -1 : property.Items.ToList().IndexOf(item);
        return property.Place is not null && index >= 0
            ? new(property, string.Join(", ", property.Items.Where((_, at) => at != index)))
            : null;
    }

    /// <summary>Флажок строки с флагом переключили: значение пишется словом прежнего.</summary>
    private static void OnToggled(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is CheckBox { IsChecked: { } isChecked } box)
        {
            ((MdPropertiesPresenter)sender).Request(
                box,
                property => property.Type == MdPropertyType.Bool && property.Place is not null && isChecked != property.IsChecked
                    ? new PropertyEdit(property, FrontMatterProperties.FlagWord(isChecked, property.ValueText))
                    : null);
        }
    }

    private static void OnEditableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var presenter = (MdPropertiesPresenter)d;
        presenter.SetValue(IsEditingPropertyKey, (bool)presenter.GetValue(ControlsEditableProperty) && (bool)presenter.GetValue(FrontMatterEditableProperty));
    }

    /// <summary>Свойство строки, в которой элемент: ближайший предок со строкой карточки в контексте данных.</summary>
    private static PropertyVm? PropertyOf(object source)
    {
        for (var node = source as DependencyObject; node is not null; node = node is Visual ? VisualTreeHelper.GetParent(node) : LogicalTreeHelper.GetParent(node))
        {
            if (node is FrameworkElement { DataContext: PropertyVm property })
            {
                return property;
            }
        }

        return null;
    }

    /// <summary>Просит записать правку свойства той строки, откуда пришла команда; набранное в поле после неё стирается.</summary>
    private void Request(object source, Func<PropertyVm, object?> edit, bool clear = false)
    {
        if (IsEditing && DataContext is PropertiesVm card && PropertyOf(source) is { } property && edit(property) is { } state)
        {
            MarkdownViewer.Owning(this)?.RequestSourceEdit(card, state);
            if (clear && source is TextBox box)
            {
                box.Clear();
            }
        }
    }

    /// <summary>Просит добавить свойство с ключом из строки добавления; набранный ключ стирается.</summary>
    private void Add(string? key)
    {
        if (IsEditing && DataContext is PropertiesVm card && key?.Trim() is { Length: > 0 } text)
        {
            MarkdownViewer.Owning(this)?.RequestSourceEdit(card, new PropertyAddition(text));
            if (GetTemplateChild("PART_NewKey") is ComboBox box)
            {
                box.Text = string.Empty;
            }
        }
    }
}
