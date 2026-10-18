namespace Beresta.Markdown.Wpf.Selection;

using System.Windows.Media;

/// <summary>A highlight of a piece of the text map: a search match, a diff change and the like.</summary>
public sealed record MdDecoration(MdTextRange Range, Brush Brush);