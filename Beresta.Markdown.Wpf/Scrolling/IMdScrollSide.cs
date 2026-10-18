namespace Beresta.Markdown.Wpf.Scrolling;

using System.Windows.Controls;

/// <summary>A side of scrolling together: source lines in its own scrolling — a viewer, an editor.</summary>
public interface IMdScrollSide
{
    /// <summary>The own scrolling of the side; until it is there — <c>null</c>.</summary>
    ScrollViewer? Scroller { get; }

    /// <summary>
    /// The source line visible at the fraction <paramref name="viewportFraction"/> of the height of
    /// its scrolling: 0 is the top edge, 1 the bottom one; the fraction is a part of the line.
    /// </summary>
    double? GetVisibleSourceLine(double viewportFraction = 0);

    /// <summary>
    /// Scrolls its scrolling so that a source line stands at the fraction
    /// <paramref name="viewportFraction"/> of its height.
    /// </summary>
    bool ScrollToSourceLine(double line, double viewportFraction = 0);
}
