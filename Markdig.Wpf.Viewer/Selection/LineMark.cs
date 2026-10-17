namespace MarkdigWpfViewer.Selection;

using System.Windows.Media;

/// <summary>Пометка строк, разложенная по карте: первая и последняя строка исходника и кисть, если она рисуется.</summary>
internal readonly record struct LineMark(MdMark Mark, int First, int Last, Brush? Brush);
