namespace MarkdigWpfViewer.ViewModels;

/// <summary>
/// Кусок исходного текста: символы с <see cref="Start"/> длиной <see cref="Length"/> и строки
/// с <see cref="StartLine"/> по <see cref="EndLine"/>, считая с нуля. <c>default</c> — позиция
/// неизвестна.
/// </summary>
public readonly record struct MdSourceSpan(int Start, int Length, int StartLine, int EndLine)
{
    /// <summary>Позиция символа сразу за куском.</summary>
    public int End => Start + Length;

    /// <summary>Позиция неизвестна.</summary>
    public bool IsEmpty => Length == 0;
}
