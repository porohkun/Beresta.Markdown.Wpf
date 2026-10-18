namespace MarkdigWpfViewer.Showcase.Localization;

using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

/// <summary>Строка витрины по ключу из привязки, <c>{loc:TextOf Путь}</c>: меняется вместе с ключом и с языком.</summary>
public sealed class TextOfExtension : MarkupExtension, IMultiValueConverter
{
    public TextOfExtension()
    {
    }

    public TextOfExtension(string path)
    {
        Path = path;
    }

    /// <summary>Путь привязки к ключу строки.</summary>
    [ConstructorArgument("path")]
    public string Path { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
        => new MultiBinding
        {
            Bindings =
            {
                new Binding(Path),
                new Binding(nameof(LocalizedStrings.Culture)) { Source = LocalizedStrings.Instance }
            },
            Converter = this
        }.ProvideValue(serviceProvider);

    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        => values[0] is string key ? Localizer.Text(key) : DependencyProperty.UnsetValue;

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
