namespace Beresta.Markdown.Wpf.Selection;

/// <summary>
/// Конец отслеживаемого куска: самый глубокий блок с идентификатором, смещение в его тексте и текст
/// блока по обе стороны. Свой текст куска у начала — справа, у конца — слева.
/// </summary>
internal readonly record struct TrackedEnd(long NodeId, int Offset, string Before, string After, bool IsStart);
