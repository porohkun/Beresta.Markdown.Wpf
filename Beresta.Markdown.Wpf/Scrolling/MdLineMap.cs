namespace Beresta.Markdown.Wpf.Scrolling;

/// <summary>
/// A correspondence of the lines of two sources by anchor pairs — for example, by the unchanged
/// lines of a diff. Between anchors lines are mapped linearly, beyond the outer ones — with the
/// shift of the outer anchor.
/// </summary>
public sealed class MdLineMap
{
    private readonly (double Left, double Right)[] _anchors;

    /// <summary>A map by anchor pairs.</summary>
    /// <param name="anchors">Pairs of lines, increasing in both sources.</param>
    public MdLineMap(IEnumerable<(double Left, double Right)> anchors)
    {
        _anchors = anchors.OrderBy(anchor => anchor.Left).ToArray();
    }

    /// <summary>Lines correspond one to one.</summary>
    public static MdLineMap Identity { get; } = new([]);

    /// <summary>Опорные пары по возрастанию; пусто — строки совпадают один к одному.</summary>
    internal IReadOnlyList<(double Left, double Right)> Anchors => _anchors;

    /// <summary>The line of the right source for a line of the left one.</summary>
    public double ToRight(double left) => Map(left, anchor => anchor.Left, anchor => anchor.Right);

    /// <summary>The line of the left source for a line of the right one.</summary>
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