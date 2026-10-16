namespace MarkdigWpfViewer.Kit;

using Markdig.Renderers;
using Markdig.Renderers.Html;

/// <summary>Тег в HTML — ссылкой <c>tag:</c>.</summary>
internal sealed class TagHtmlRenderer : HtmlObjectRenderer<TagInline>
{
    protected override void Write(HtmlRenderer renderer, TagInline tag)
        => renderer.Write("<a href=\"").WriteEscapeUrl("tag:" + tag.Name).Write("\">#").WriteEscape(tag.Name).Write("</a>");
}
