namespace Beresta.Markdown.Wpf.ViewModels;

/// <summary>Перенос строки; жёсткий в исходнике помечен обратной чертой или двумя пробелами.</summary>
public sealed record LineBreakVm(bool IsHard = false) : InlineVm;