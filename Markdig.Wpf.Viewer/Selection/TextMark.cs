namespace MarkdigWpfViewer.Selection;

using System.Windows.Media;

/// <summary>Пометка куска текста, разложенная по карте: её кусок и кисть, если она рисуется.</summary>
internal readonly record struct TextMark(MdMark Mark, MdTextRange Range, Brush? Brush);
