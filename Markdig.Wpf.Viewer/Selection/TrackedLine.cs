namespace MarkdigWpfViewer.Selection;

/// <summary>
/// Край отслеживаемых строк: блок и сдвиг строки от него — внутри блока от его первой строки, вне
/// блоков от первой строки следующего или, с <paramref name="FromEnd"/>, от последней строки
/// предыдущего, — и текст строки с соседями.
/// </summary>
internal readonly record struct TrackedLine(
    long NodeId,
    int Delta,
    bool Inside,
    bool FromEnd,
    string? Text,
    string? Previous,
    string? Next);
