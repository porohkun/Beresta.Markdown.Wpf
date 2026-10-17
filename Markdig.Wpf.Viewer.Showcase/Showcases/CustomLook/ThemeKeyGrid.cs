namespace MarkdigWpfViewer.Showcase.Showcases.CustomLook;

using System.Windows;
using System.Windows.Controls;
using HandyControl.Controls;
using HandyControl.Data;

/// <summary>Сетка свойств HandyControl с редакторами ключей темы и поиском по имени ключа без учёта регистра.</summary>
public sealed class ThemeKeyGrid : PropertyGrid
{
    private ItemsControl? _items;
    private SearchBar? _search;

    public ThemeKeyGrid()
    {
        // BYDESIGN: неявный стиль HandyControl ищется по типу контрола, и наследник его сам не находит.
        SetResourceReference(StyleProperty, typeof(PropertyGrid));
    }

    public override PropertyResolver PropertyResolver { get; } = new ThemeKeyResolver();

    public override void OnApplyTemplate()
    {
        if (_search is not null)
        {
            _search.SearchStarted -= OnSearchStarted;
        }

        base.OnApplyTemplate();
        _items = GetTemplateChild("PART_ItemsControl") as ItemsControl;
        _search = GetTemplateChild("PART_SearchBar") as SearchBar;
        if (_search is not null)
        {
            _search.SearchStarted += OnSearchStarted;
        }
    }

    protected override void OnSelectedObjectChanged(object oldValue, object newValue)
    {
        base.OnSelectedObjectChanged(oldValue, newValue);
        Filter(_search?.Text);
    }

    // BYDESIGN: поиск сетки сравнивает имена в нижнем регистре с запросом как есть, и запрос с заглавной
    // буквой ничего не находит; после её поиска строки показываются заново.
    private void OnSearchStarted(object? sender, FunctionEventArgs<string> e) => Filter(e.Info);

    /// <summary>Показывает ключи, в имени которых есть запрос, остальные прячет.</summary>
    private void Filter(string? query)
    {
        foreach (var property in _items?.Items.OfType<PropertyItem>() ?? [])
        {
            property.Visibility = property.DisplayName.Contains(query ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }
}
