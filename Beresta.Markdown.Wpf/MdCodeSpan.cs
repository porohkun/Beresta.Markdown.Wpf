namespace Beresta.Markdown.Wpf;

/// <summary>
/// A piece of code from <paramref name="Start"/> of length <paramref name="Length"/> with its look:
/// <paramref name="Look"/> <c>X</c> is drawn with the style <c>Md.Code.X.Style</c>.
/// </summary>
public readonly record struct MdCodeSpan(int Start, int Length, string Look);
