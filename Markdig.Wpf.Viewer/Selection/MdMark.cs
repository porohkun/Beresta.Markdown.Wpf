namespace MarkdigWpfViewer.Selection;

using System.Windows.Media;

/// <summary>Пометка куска текстовой карты или строк исходника: вид, оформление и данные приложения.</summary>
public sealed record MdMark
{
    /// <summary>Пометка куска текстовой карты.</summary>
    public MdMark(MdTextRange range, MdMarkKind kind = MdMarkKind.Fill)
    {
        Range = range;
        Kind = kind;
    }

    /// <summary>Пометка строк исходника с <paramref name="startLine"/> по <paramref name="endLine"/> включительно.</summary>
    public MdMark(int startLine, int endLine, MdMarkKind kind = MdMarkKind.Row)
    {
        Lines = (Math.Min(startLine, endLine), Math.Max(startLine, endLine));
        Kind = kind;
    }

    /// <summary>Кусок текстовой карты; у пометки строк — <c>null</c>.</summary>
    public MdTextRange? Range { get; init; }

    /// <summary>Строки исходника включительно; у пометки куска — <c>null</c>.</summary>
    public (int Start, int End)? Lines { get; init; }

    public MdMarkKind Kind { get; init; }

    /// <summary>Кисть; не задана — кисть темы по имени <see cref="Style"/>.</summary>
    public Brush? Brush { get; init; }

    /// <summary>
    /// Имя оформления в теме: заливка и строки берут кисть из ключа <c>Md.Mark.{Style}.Brush</c>,
    /// черта, рамка и поля — из <c>Md.Mark.{Style}.LineBrush</c>.
    /// </summary>
    public string? Style { get; init; }

    /// <summary>Значок вида <see cref="MdMarkKind.Icon"/> — фигура, залитая кистью пометки.</summary>
    public Geometry? Icon { get; init; }

    /// <summary>Данные приложения; приходят вместе с пометкой из <see cref="MarkdownViewer.GetMarksAt(System.Windows.Point)"/>.</summary>
    public object? Tag { get; init; }
}
