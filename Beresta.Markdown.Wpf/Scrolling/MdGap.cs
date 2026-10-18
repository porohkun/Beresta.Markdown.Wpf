namespace Beresta.Markdown.Wpf.Scrolling;

/// <summary>
/// An empty place of height <paramref name="Height"/> before the source line
/// <paramref name="Line"/>.
/// </summary>
public sealed record MdGap(int Line, double Height);
