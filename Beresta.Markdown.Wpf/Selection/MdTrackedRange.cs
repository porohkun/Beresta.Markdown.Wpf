namespace Beresta.Markdown.Wpf.Selection;

/// <summary>
/// A piece of the text map that finds its place in the map of new text — by its blocks and by the
/// text at its ends. It vanishes together with its block.
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
