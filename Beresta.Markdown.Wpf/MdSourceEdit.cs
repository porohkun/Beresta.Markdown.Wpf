namespace Beresta.Markdown.Wpf;

/// <summary>
/// A change of the source: <see cref="Length"/> characters from <see cref="Start"/> are replaced with
/// <see cref="Text"/>; a zero length inserts, an empty text removes.
/// </summary>
public readonly record struct MdSourceEdit(int Start, int Length, string Text)
{
    /// <summary>The text with the change applied.</summary>
    public string ApplyTo(string source)
        => string.Concat(source.AsSpan(0, Start), Text.AsSpan(), source.AsSpan(Start + Length));
}
