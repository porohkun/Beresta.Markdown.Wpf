namespace Beresta.Markdown.Wpf.Kit;

using Markdig.Renderers;
using Markdig.Renderers.Html;

/// <summary>Шорткод в HTML — именем и аргументами текстом.</summary>
internal sealed class ShortcodeHtmlRenderer : HtmlObjectRenderer<ShortcodeInline>
{
    protected override void Write(HtmlRenderer renderer, ShortcodeInline inline)
        => renderer.WriteEscape($"{{% {inline.Shortcode.Name} {string.Join(" ", inline.Shortcode.Arguments)} %}}");
}
