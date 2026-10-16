namespace MarkdigWpfViewer.Kit;

using System.Windows;
using Extensions;
using Markdig.Syntax.Inlines;
using ViewModels;

/// <summary>
/// Цветовые чипы: код в строке, который целиком — цвет (<c>#RGB</c>, <c>#RRGGBB</c>, <c>#RRGGBBAA</c>,
/// <c>rgb()</c>, <c>rgba()</c>, <c>hsl()</c>, <c>hsla()</c>), получает кружок этого цвета. Вид — шаблон
/// темы <c>Md.ColorCode.Template</c>.
/// </summary>
public sealed class ColorChips : MdExtension
{
    public override InlineVm? ConvertInline(Inline inline, MdConvertContext context)
        => inline is CodeInline { Content: { } code } && CssColor.TryParse(code, out var color) ? new ColorCodeVm(code, color) : null;

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item is ColorCodeVm && container is FrameworkElement element ? element.TryFindResource("Md.ColorCode.Template") as DataTemplate : null;
}
