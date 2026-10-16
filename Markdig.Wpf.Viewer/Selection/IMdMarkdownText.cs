namespace MarkdigWpfViewer.Selection;

/// <summary>Строчный виджет, который пишет в разметку выделенного свой текст вместо исходника узла.</summary>
public interface IMdMarkdownText
{
    /// <summary>Текст для разметки выделенного; пустой — виджет в неё не попадает.</summary>
    string MarkdownText { get; }
}
