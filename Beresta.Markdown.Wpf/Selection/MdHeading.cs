namespace Beresta.Markdown.Wpf.Selection;

/// <summary>Заголовок документа для оглавления.</summary>
/// <param name="Level">Уровень от 1 до 6.</param>
/// <param name="Text">Текст заголовка без разметки.</param>
/// <param name="Id">Якорь, как у GitHub: по нему ведёт ссылка <c>#id</c>.</param>
/// <param name="Range">Кусок текстовой карты заголовка.</param>
/// <param name="SourceLine">Строка исходника, считая с нуля.</param>
public sealed record MdHeading(int Level, string Text, string Id, MdTextRange Range, int SourceLine)
{
    /// <summary>Ссылка на заголовок: <c>#id</c>.</summary>
    public string Href => "#" + Id;
}