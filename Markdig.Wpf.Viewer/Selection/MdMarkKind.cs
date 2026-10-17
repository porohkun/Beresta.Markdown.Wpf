namespace MarkdigWpfViewer.Selection;

/// <summary>Как рисуется пометка.</summary>
public enum MdMarkKind
{
    /// <summary>Заливка текста.</summary>
    Fill,

    /// <summary>Черта под текстом.</summary>
    Underline,

    /// <summary>Рамка вокруг текста, у каждой экранной строки своя.</summary>
    Frame,

    /// <summary>Строки во всю ширину документа.</summary>
    Row,

    /// <summary>Полоса на полях у номеров строк.</summary>
    Bar,

    /// <summary>Значок на полях у номера первой строки.</summary>
    Icon,

    /// <summary>Вертикальная черта в начале куска; у пустого куска — на его месте.</summary>
    Caret
}
