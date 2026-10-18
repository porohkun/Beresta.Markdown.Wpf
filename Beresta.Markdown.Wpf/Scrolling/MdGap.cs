namespace Beresta.Markdown.Wpf.Scrolling;

/// <summary>Пустое место высотой <paramref name="Height"/> перед строкой исходника <paramref name="Line"/>.</summary>
public sealed record MdGap(int Line, double Height);
