namespace Beresta.Markdown.Wpf.Scrolling;

/// <summary>
/// Source lines of an element that stands in the document in place of blocks: where each line is in
/// the coordinates of the element.
/// </summary>
public interface IMdSourceRows
{
    /// <summary>The lines moved inside the element: the viewer redraws what stands at them — line numbers, marks.</summary>
    event EventHandler? RowsChanged;

    /// <summary>
    /// The top of a source line in the coordinates of the element; the fraction is a part of the
    /// line. Unknown — <c>null</c>.
    /// </summary>
    double? GetSourceLineOffset(double line);

    /// <summary>
    /// The source line at a position of the element; the fraction is a part of the line. Unknown —
    /// <c>null</c>.
    /// </summary>
    double? GetSourceLineAt(double offset);

    /// <summary>
    /// Source lines that start in the visible part of the element: the top and height of the screen
    /// row each one starts with, in the coordinates of the element.
    /// </summary>
    IReadOnlyList<(int Line, double Top, double Height)> VisibleRows();
}
