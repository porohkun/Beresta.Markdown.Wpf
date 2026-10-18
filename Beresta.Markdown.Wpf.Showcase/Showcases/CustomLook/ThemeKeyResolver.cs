namespace Beresta.Markdown.Wpf.Showcase.Showcases.CustomLook;

using System.Windows;
using System.Windows.Media;
using HandyControl.Controls;

/// <summary>Редакторы ключей темы по типу значения: цвет — пикером, число — счётчиком, остальное — текстом.</summary>
public sealed class ThemeKeyResolver : PropertyResolver
{
    public override PropertyEditorBase CreateDefaultEditor(Type type)
    {
        if (type == typeof(SolidColorBrush))
        {
            return new ColorPropertyEditor();
        }

        if (type == typeof(double))
        {
            return new NumberPropertyEditor(0, double.MaxValue);
        }

        return type == typeof(Thickness) || type == typeof(CornerRadius) || type == typeof(GridLength)
            ? new PlainTextPropertyEditor()
            : base.CreateDefaultEditor(type);
    }
}
