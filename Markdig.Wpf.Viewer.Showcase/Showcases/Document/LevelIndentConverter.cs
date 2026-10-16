namespace MarkdigWpfViewer.Showcase.Showcases.Document;

using System.Globalization;
using System.Windows;
using System.Windows.Data;

/// <summary>Отступ пункта оглавления по уровню заголовка.</summary>
public sealed class LevelIndentConverter : IValueConverter
{
    private const double Step = 12;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => new Thickness(value is int level ? (level - 1) * Step : 0, 0, 0, 0);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
