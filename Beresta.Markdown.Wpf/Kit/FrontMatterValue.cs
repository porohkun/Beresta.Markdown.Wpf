namespace Beresta.Markdown.Wpf.Kit;

/// <summary>
/// Где значение свойства в строках front matter и как оно записано. У значения в строке ключа место —
/// от двоеточия до конца значения без комментария; у списка строками — от двоеточия до конца его
/// последней строки.
/// </summary>
/// <param name="Kind">Как записано значение.</param>
/// <param name="Line">Строка ключа, с нуля среди строк front matter.</param>
/// <param name="Start">Колонка сразу после двоеточия.</param>
/// <param name="LastLine">Последняя строка значения.</param>
/// <param name="End">Колонка конца значения в последней строке.</param>
/// <param name="Raw">Значение как написано.</param>
/// <param name="Indent">Отступ пунктов списка строками.</param>
internal sealed record FrontMatterValue(MdValueKind Kind, int Line, int Start, int LastLine, int End, string Raw, string Indent);
