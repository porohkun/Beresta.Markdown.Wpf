namespace MarkdigWpfViewer.Showcase.Showcases.CustomLook;

using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

/// <summary>Свойства <see cref="ThemeKeys"/>: по свойству на цвет, размер и отступ темы вьюера.</summary>
public sealed class ThemeKeysProvider : TypeDescriptionProvider
{
    private static readonly Uri ThemeSource = new("pack://application:,,,/Markdig.Wpf.Viewer;component/Themes/Generic.xaml");

    private static readonly Lazy<PropertyDescriptorCollection> Keys = new(Collect);

    public ThemeKeysProvider()
        : base(TypeDescriptor.GetProvider(typeof(object)))
    {
    }

    public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object? instance)
        => new KeysDescriptor(base.GetTypeDescriptor(objectType, instance));

    /// <summary>Ключи темы вьюера из ресурсов приложения, по имени.</summary>
    private static PropertyDescriptorCollection Collect()
    {
        var theme = Application.Current?.Resources.MergedDictionaries.FirstOrDefault(dictionary => dictionary.Source == ThemeSource);
        if (theme is null)
        {
            return PropertyDescriptorCollection.Empty;
        }

        var keys = theme.Keys
            .OfType<string>()
            .Where(key => key.StartsWith("Md.", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .Select(key => (Key: key, Type: theme[key]?.GetType()))
            .Where(entry => entry.Type is not null && GroupOf(entry.Type) is not null)
            .Select(entry => (PropertyDescriptor)new ThemeKeyDescriptor(entry.Key, entry.Type!, GroupOf(entry.Type!)!))
            .ToArray();
        return new(keys, readOnly: true);
    }

    /// <summary>Группа ключа по типу значения; значение, которое панель не правит, — <c>null</c>.</summary>
    private static string? GroupOf(Type type)
        => type == typeof(SolidColorBrush) ? "Цвета"
            : type == typeof(Thickness) ? "Отступы"
            : type == typeof(double) || type == typeof(CornerRadius) || type == typeof(GridLength) ? "Размеры"
            : null;

    /// <summary>Описание типа со свойствами-ключами.</summary>
    private sealed class KeysDescriptor : CustomTypeDescriptor
    {
        public KeysDescriptor(ICustomTypeDescriptor? parent)
            : base(parent)
        {
        }

        public override PropertyDescriptorCollection GetProperties() => Keys.Value;

        public override PropertyDescriptorCollection GetProperties(Attribute[]? attributes) => Keys.Value;
    }
}
