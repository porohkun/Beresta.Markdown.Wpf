namespace Beresta.Markdown.Wpf.Kit;

using Markdig.Renderers;
using Markdig.Renderers.Html;

/// <summary>Спойлер в HTML — <c>&lt;span class="spoiler"&gt;</c>.</summary>
internal sealed class SpoilerHtmlRenderer : HtmlObjectRenderer<SpoilerInline>
{
    protected override void Write(HtmlRenderer renderer, SpoilerInline spoiler)
        => renderer.Write("<span class=\"spoiler\">").WriteEscape(spoiler.Text).Write("</span>");
}
