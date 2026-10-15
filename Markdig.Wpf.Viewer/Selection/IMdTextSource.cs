namespace MarkdigWpfViewer.Selection;

/// <summary>
/// Виджет, который отдаёт в текстовую карту свой текст и вложенные блоки. Виджет без него
/// неделимый: выделяется и копируется целиком, а текста в карте у него нет.
/// </summary>
public interface IMdTextSource
{
    void WriteText(MdTextWriter writer);
}