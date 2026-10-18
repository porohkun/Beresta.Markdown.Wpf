namespace Beresta.Markdown.Wpf.Selection;

using System.Windows.Media;

/// <summary>
/// Кисти пометок по именам оформления из темы вьюера: заливка — ключ <c>Md.Mark.{имя}.Brush</c>,
/// черта, рамка и поля — <c>Md.Mark.{имя}.LineBrush</c>, а без него та же заливка.
/// </summary>
internal sealed class ThemeBrushes
{
    private readonly Action<ThemeBrush> _adopt;
    private readonly Dictionary<string, ThemeBrush> _brushes = new(StringComparer.Ordinal);

    /// <param name="adopt">Ставит кисть в логическое дерево вьюера: оттуда она находит ресурсы темы.</param>
    public ThemeBrushes(Action<ThemeBrush> adopt)
    {
        _adopt = adopt;
    }

    /// <summary>Кисть одного из ключей сменилась — сменилась тема.</summary>
    public event Action? Changed;

    public IEnumerable<ThemeBrush> All => _brushes.Values;

    /// <summary>Кисть имени для вида пометки.</summary>
    public Brush? BrushOf(string name, MdMarkKind kind)
    {
        var fill = Follow($"Md.Mark.{name}.Brush");
        return kind is MdMarkKind.Fill or MdMarkKind.Row ? fill : Follow($"Md.Mark.{name}.LineBrush") ?? fill;
    }

    private Brush? Follow(string key)
    {
        if (!_brushes.TryGetValue(key, out var brush))
        {
            brush = new(key);
            _adopt(brush);
            brush.Follow();
            brush.Changed += () => Changed?.Invoke();
            _brushes[key] = brush;
        }

        return brush.Brush;
    }
}
