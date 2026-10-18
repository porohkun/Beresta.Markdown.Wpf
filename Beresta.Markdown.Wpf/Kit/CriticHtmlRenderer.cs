namespace Beresta.Markdown.Wpf.Kit;

using Markdig.Renderers;
using Markdig.Renderers.Html;

/// <summary>CriticMarkup в HTML: <c>&lt;ins&gt;</c>, <c>&lt;del&gt;</c>, <c>&lt;mark&gt;</c>; комментарий — HTML-комментарием.</summary>
internal sealed class CriticHtmlRenderer : HtmlObjectRenderer<CriticInline>
{
    protected override void Write(HtmlRenderer renderer, CriticInline critic)
    {
        switch (critic.Kind)
        {
            case CriticKind.Insertion:
                renderer.Write("<ins>").WriteEscape(critic.Text).Write("</ins>");
                break;

            case CriticKind.Deletion:
                renderer.Write("<del>").WriteEscape(critic.Text).Write("</del>");
                break;

            case CriticKind.Substitution:
                renderer.Write("<del>").WriteEscape(critic.Text).Write("</del><ins>").WriteEscape(critic.Replacement).Write("</ins>");
                break;

            case CriticKind.Highlight:
                renderer.Write("<mark>").WriteEscape(critic.Text).Write("</mark>");
                break;

            default:
                renderer.Write("<!-- ").WriteEscape(critic.Text.Replace("--", "- -")).Write(" -->");
                break;
        }
    }
}
