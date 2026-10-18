namespace Beresta.Markdown.Wpf.Scrolling;

/// <summary>
/// Соответствие строк двух исходников по опорным парам — например, по неизменённым строкам диффа.
/// Между опорами строки пересчитываются линейно, за крайними — со сдвигом крайней опоры.
/// </summary>
public sealed class MdLineMap
{
    private readonly (double Left, double Right)[] _anchors;

    /// <param name="anchors">Пары строк, возрастающие в обоих исходниках.</param>
    public MdLineMap(IEnumerable<(double Left, double Right)> anchors)
    {
        _anchors = anchors.OrderBy(anchor => anchor.Left).ToArray();
    }

    /// <summary>Строки совпадают один к одному.</summary>
    public static MdLineMap Identity { get; } = new([]);

    /// <summary>Опорные пары по возрастанию; пусто — строки совпадают один к одному.</summary>
    internal IReadOnlyList<(double Left, double Right)> Anchors => _anchors;

    /// <summary>Строка правого исходника для строки левого.</summary>
    public double ToRight(double left) => Map(left, anchor => anchor.Left, anchor => anchor.Right);

    /// <summary>Строка левого исходника для строки правого.</summary>
    public double ToLeft(double right) => Map(right, anchor => anchor.Right, anchor => anchor.Left);

    private double Map(double value, Func<(double Left, double Right), double> from, Func<(double Left, double Right), double> to)
    {
        if (_anchors.Length == 0)
        {
            return value;
        }

        if (value <= from(_anchors[0]))
        {
            return to(_anchors[0]) - (from(_anchors[0]) - value);
        }

        for (var i = 1; i < _anchors.Length; i++)
        {
            var previous = _anchors[i - 1];
            var next = _anchors[i];
            if (value > from(next))
            {
                continue;
            }

            var span = from(next) - from(previous);
            var fraction = span > 0 ? (value - from(previous)) / span : 0;
            return to(previous) + fraction * (to(next) - to(previous));
        }

        return to(_anchors[^1]) + (value - from(_anchors[^1]));
    }
}