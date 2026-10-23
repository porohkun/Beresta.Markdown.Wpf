namespace Beresta.Markdown.Wpf.Showcase.Showcases.Editor;

using System.Globalization;
using System.Windows.Data;

/// <summary>Отмечен ли переключатель режима: режим совпадает с параметром; отмеченный ставит свой режим.</summary>
public sealed class EditModeIsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => Equals(value, parameter);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? parameter : Binding.DoNothing;
}
