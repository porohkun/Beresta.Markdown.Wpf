namespace MarkdigWpfViewer.Selection;

using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

/// <summary>
/// Последний рендерер HTML: узел, который не взял ни один другой, пишется исходным текстом — строчный
/// как есть, блочный абзацем; у контейнера пишется содержимое.
/// </summary>
internal sealed class SourceHtmlRenderer(string markdown) : HtmlObjectRenderer<MarkdownObject>
{
    protected override void Write(HtmlRenderer renderer, MarkdownObject node)
    {
        switch (node)
        {
            case ContainerBlock container:
                renderer.WriteChildren(container);
                break;

            case ContainerInline container:
                renderer.WriteChildren(container);
                break;

            case Block:
                renderer.EnsureLine().Write("<p>").WriteEscape(SourceOf(node)).Write("</p>").WriteLine();
                break;

            default:
                renderer.WriteEscape(SourceOf(node));
                break;
        }
    }

    private string SourceOf(MarkdownObject node)
        => node.Span.IsEmpty || node.Span.Start < 0 || node.Span.End >= markdown.Length
            ? string.Empty
            : markdown.Substring(node.Span.Start, node.Span.Length);
}
