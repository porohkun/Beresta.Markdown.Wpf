namespace Beresta.Markdown.Wpf.Selection;

/// <summary>
/// Source lines that find their place in new text — by the blocks they stand in and by the text of
/// the lines. They vanish together with their block.
/// </summary>
public sealed class MdTrackedLines
{
    internal MdTrackedLines(TrackedLine? start, TrackedLine? end)
    {
        Start = start;
        End = end;
    }

    internal TrackedLine? Start { get; }

    internal TrackedLine? End { get; }
}
