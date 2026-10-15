namespace MarkdigWpfViewer.Selection;

/// <summary>
/// Неделимый виджет с текстом для копирования: в текстовой карте он — один знак-заместитель,
/// а в скопированный текст попадает <see cref="CopyText"/>.
/// </summary>
public interface IMdCopyText
{
    string CopyText { get; }
}
