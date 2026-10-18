namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>
/// A piece of the source text: characters from <see cref="Start"/> of length <see cref="Length"/>
/// and lines from <see cref="StartLine"/> to <see cref="EndLine"/>, counting from zero.
/// <c>default</c> — the position is unknown.
/// </summary>
public readonly record struct MdSourceSpan(int Start, int Length, int StartLine, int EndLine)
{
    /// <summary>The position of the character right after the piece.</summary>
    public int End => Start + Length;

    /// <summary>The position is unknown.</summary>
    public bool IsEmpty => Length == 0;
}