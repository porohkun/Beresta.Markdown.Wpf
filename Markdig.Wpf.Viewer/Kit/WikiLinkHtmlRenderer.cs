namespace MarkdigWpfViewer.Kit;

using Markdig.Renderers;
using Markdig.Renderers.Html;

/// <summary>Вики-ссылка в HTML — ссылкой <c>wiki:</c>, встраивание — картинкой.</summary>
internal sealed class WikiLinkHtmlRenderer : HtmlObjectRenderer<WikiLinkInline>
{
    protected override void Write(HtmlRenderer renderer, WikiLinkInline link)
    {
        if (link.IsEmbed)
        {
            renderer.Write("<img src=\"").WriteEscapeUrl(link.Target).Write("\" alt=\"").WriteEscape(link.Target).Write("\" />");
            return;
        }

        renderer.Write("<a href=\"").WriteEscapeUrl(WikiLinks.Href(link)).Write("\">").WriteEscape(WikiLinks.Caption(link)).Write("</a>");
    }
}
