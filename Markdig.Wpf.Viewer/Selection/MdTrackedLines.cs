namespace MarkdigWpfViewer.Selection;

/// <summary>
/// Строки исходника, которые находят своё место в новом тексте — по блокам, где они стоят, и тексту
/// строк. Пропадают вместе со своим блоком.
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
