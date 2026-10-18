namespace Beresta.Markdown.Wpf.Selection;

using System.Windows;
using System.Windows.Media;

/// <summary>Кисть темы по ключу, которая следит за сменой темы; живёт логическим потомком вьюера.</summary>
internal sealed class ThemeBrush : FrameworkContentElement
{
    public static readonly DependencyProperty BrushProperty =
        DependencyProperty.Register(
            nameof(Brush),
            typeof(Brush),
            typeof(ThemeBrush),
            new(null, (d, _) => ((ThemeBrush)d).Changed?.Invoke()));

    public ThemeBrush(string key)
    {
        Key = key;
    }

    /// <summary>Кисть по ключу сменилась.</summary>
    public event Action? Changed;

    public string Key { get; }

    public Brush? Brush => (Brush?)GetValue(BrushProperty);

    /// <summary>Начинает следить за ключом; до этого кисть уже должна стоять в дереве.</summary>
    public void Follow() => SetResourceReference(BrushProperty, Key);
}
