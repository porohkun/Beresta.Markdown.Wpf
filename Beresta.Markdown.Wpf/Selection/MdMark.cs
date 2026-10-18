namespace Beresta.Markdown.Wpf.Selection;

using System.Windows.Media;

/// <summary>
/// A mark on a piece of the text map or on source lines: the kind, the look and data of the
/// application.
/// </summary>
public sealed record MdMark
{
    /// <summary>A mark on a piece of the text map.</summary>
    public MdMark(MdTextRange range, MdMarkKind kind = MdMarkKind.Fill)
    {
        Range = range;
        Kind = kind;
    }

    /// <summary>
    /// A mark on source lines from <paramref name="startLine"/> to <paramref name="endLine"/>
    /// inclusive.
    /// </summary>
    public MdMark(int startLine, int endLine, MdMarkKind kind = MdMarkKind.Row)
    {
        Lines = (Math.Min(startLine, endLine), Math.Max(startLine, endLine));
        Kind = kind;
    }

    /// <summary>The piece of the text map; a mark on lines has <c>null</c>.</summary>
    public MdTextRange? Range { get; init; }

    /// <summary>Source lines, inclusive; a mark on a piece has <c>null</c>.</summary>
    public (int Start, int End)? Lines { get; init; }

    /// <summary>How the mark is drawn.</summary>
    public MdMarkKind Kind { get; init; }

    /// <summary>The brush; not set — the theme brush by the name <see cref="Style"/>.</summary>
    public Brush? Brush { get; init; }

    /// <summary>
    /// The name of a look in the theme: a fill and rows take the brush from the key
    /// <c>Md.Mark.{Style}.Brush</c>; a line, a frame and the margin — from
    /// <c>Md.Mark.{Style}.LineBrush</c>.
    /// </summary>
    public string? Style { get; init; }

    /// <summary>
    /// The icon of the kind <see cref="MdMarkKind.Icon"/> — a shape filled with the brush of the
    /// mark.
    /// </summary>
    public Geometry? Icon { get; init; }

    /// <summary>
    /// Data of the application; comes back with the mark from
    /// <see cref="MarkdownViewer.GetMarksAt(System.Windows.Point)"/>.
    /// </summary>
    public object? Tag { get; init; }
}
