namespace Beresta.Markdown.Wpf.Selection;

using System.Windows.Media;

/// <summary>Подсветка куска текстовой карты: найденное, правка диффа и подобное.</summary>
public sealed record MdDecoration(MdTextRange Range, Brush Brush);