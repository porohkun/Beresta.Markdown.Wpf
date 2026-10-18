namespace Beresta.Markdown.Wpf.Showcase.Showcases.CustomLook;

using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

/// <summary>
/// Ключи темы вьюера для сетки свойств: значение нынешней темы или своё. Свои лежат в
/// <see cref="Overrides"/>, который вид вливает в ресурсы вьюера.
/// </summary>
// BYDESIGN: сетка свойств HandyControl берёт свойства у типа, а не у объекта, — ключи описывает поставщик типа.
[TypeDescriptionProvider(typeof(ThemeKeysProvider))]
public sealed class ThemeKeys : INotifyPropertyChanged
{
    private readonly Dictionary<string, object> _values = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Свои значения ключей.</summary>
    public ResourceDictionary Overrides { get; } = new();

    /// <summary>Значение ключа: своё или нынешней темы.</summary>
    public object? Get(string key)
    {
        if (!_values.TryGetValue(key, out var value) && FromTheme(key) is { } theme)
        {
            _values[key] = value = theme;
        }

        return value;
    }

    /// <summary>Ставит своё значение ключа.</summary>
    public void Set(string key, object? value)
    {
        if (value is null)
        {
            return;
        }

        _values[key] = value;
        Overrides[key] = value is Freezable freezable ? freezable.GetCurrentValueAsFrozen() : value;
        PropertyChanged?.Invoke(this, new(ThemeKeyDescriptor.NameOf(key)));
    }

    /// <summary>Убирает свои значения: все ключи снова как в теме.</summary>
    public void Reset()
    {
        Overrides.Clear();
        _values.Clear();
        PropertyChanged?.Invoke(this, new(string.Empty));
    }

    /// <summary>Перечитывает из темы ключи без своих значений — после смены темы.</summary>
    public void Refresh()
    {
        foreach (var key in _values.Keys.Where(key => !Overrides.Contains(key)).ToList())
        {
            _values.Remove(key);
            PropertyChanged?.Invoke(this, new(ThemeKeyDescriptor.NameOf(key)));
        }
    }

    /// <summary>Значение темы; прозрачность кисти — в альфе её цвета.</summary>
    private static object? FromTheme(string key) => Application.Current?.TryFindResource(key) switch
    {
        SolidColorBrush brush => new SolidColorBrush(
            Color.FromArgb((byte)Math.Round(brush.Color.A * brush.Opacity), brush.Color.R, brush.Color.G, brush.Color.B)),
        var value => value
    };
}
