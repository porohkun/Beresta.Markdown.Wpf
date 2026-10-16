namespace MarkdigWpfViewer.Kit;

using Markdig.Renderers;
using Markdig.Renderers.Html;

/// <summary>Предупреждение MkDocs в HTML, как его пишет MkDocs: <c>&lt;div class="admonition вид"&gt;</c> с заголовком.</summary>
internal sealed class AdmonitionHtmlRenderer : HtmlObjectRenderer<AdmonitionBlock>
{
    protected override void Write(HtmlRenderer renderer, AdmonitionBlock block)
    {
        renderer.EnsureLine();
        renderer.Write("<div class=\"admonition ").WriteEscape(block.Kind).WriteLine("\">");
        renderer.Write("<p class=\"admonition-title\">").WriteEscape(block.Title ?? block.Kind).WriteLine("</p>");
        renderer.WriteChildren(block);
        renderer.WriteLine("</div>");
    }
}
