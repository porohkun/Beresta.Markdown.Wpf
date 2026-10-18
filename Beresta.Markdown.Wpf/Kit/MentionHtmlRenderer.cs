namespace Beresta.Markdown.Wpf.Kit;

using Markdig.Renderers;
using Markdig.Renderers.Html;

/// <summary>Упоминание в HTML — ссылкой <c>mention:</c>.</summary>
internal sealed class MentionHtmlRenderer : HtmlObjectRenderer<MentionInline>
{
    protected override void Write(HtmlRenderer renderer, MentionInline mention)
        => renderer.Write("<a href=\"").WriteEscapeUrl("mention:" + mention.Name).Write("\">@").WriteEscape(mention.Name).Write("</a>");
}
