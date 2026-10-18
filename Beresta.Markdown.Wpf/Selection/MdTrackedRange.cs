namespace Beresta.Markdown.Wpf.Selection;

/// <summary>
/// Кусок текстовой карты, который находит своё место в карте нового текста — по своим блокам и
/// тексту у концов. Пропадает вместе со своим блоком.
/// </summary>
public sealed class MdTrackedRange
{
    internal MdTrackedRange(TrackedEnd? start, TrackedEnd? end)
    {
        Start = start;
        End = end;
    }

    internal TrackedEnd? Start { get; }

    internal TrackedEnd? End { get; }
}
