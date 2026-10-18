namespace Beresta.Markdown.Wpf.Kit;

using System.Windows;
using Extensions;
using Markdig.Syntax.Inlines;
using ViewModels;

/// <summary>
/// Color chips: inline code that is a color as a whole (<c>#RGB</c>, <c>#RRGGBB</c>,
/// <c>#RRGGBBAA</c>, <c>rgb()</c>, <c>rgba()</c>, <c>hsl()</c>, <c>hsla()</c>) gets a dot of that
/// color. The look is the theme template <c>Md.ColorCode.Template</c>.
/// </summary>
public sealed class ColorChips : MdExtension
{
    /// <inheritdoc/>
    public override InlineVm? ConvertInline(Inline inline, MdConvertContext context)
        => inline is CodeInline { Content: { } code } && CssColor.TryParse(code, out var color) ? new ColorCodeVm(code, color) : null;

    /// <inheritdoc/>
    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item is ColorCodeVm && container is FrameworkElement element ? element.TryFindResource("Md.ColorCode.Template") as DataTemplate : null;
}
