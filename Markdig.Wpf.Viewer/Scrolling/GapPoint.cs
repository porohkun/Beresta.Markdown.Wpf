namespace MarkdigWpfViewer.Scrolling;

using ViewModels;

/// <summary>
/// Куда встаёт пустое место строки: элемент и место относительно него; внутри текста или кода —
/// смещение экранной строки в тексте элемента.
/// </summary>
internal readonly record struct GapPoint(GapPlace Place, NodeVm Node, int Offset)
{
    /// <summary>Совпадает ли место с другим; у строк внутри текста места свои.</summary>
    public bool Coincides(GapPoint other)
        => Place != GapPlace.Row && Place == other.Place && ReferenceEquals(Node, other.Node);
}
