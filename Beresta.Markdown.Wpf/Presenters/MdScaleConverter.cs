namespace Beresta.Markdown.Wpf.Presenters;

using System.Globalization;
using System.Windows.Data;

/// <summary>Число, умноженное на постоянный множитель: размер шрифта индекса от размера текста.</summary>
internal sealed class MdScaleConverter(double factor) : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is double number ? number * factor : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is double number ? number / factor : value;
}