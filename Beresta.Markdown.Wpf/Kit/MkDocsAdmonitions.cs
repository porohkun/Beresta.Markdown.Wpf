namespace Beresta.Markdown.Wpf.Kit;

using Extensions;
using Markdig;
using Markdig.Syntax;
using ViewModels;

/// <summary>
/// Предупреждения MkDocs: <c>!!! вид "Заголовок"</c> и содержимое с отступом в четыре пробела;
/// <c>???</c> — свёрнутое, <c>???+</c> — развёрнутое сворачиваемое.
/// </summary>
public sealed class MkDocsAdmonitions : MdExtension
{
    public override void SetupPipeline(MarkdownPipelineBuilder pipeline)
        => pipeline.Extensions.AddIfNotAlready<AdmonitionMarkdigExtension>();

    public override BlockVm? ConvertBlock(Block block, MdConvertContext context)
    {
        if (block is not AdmonitionBlock admonition)
        {
            return null;
        }

        var heading = CalloutKinds.FirstLine(admonition, context).Span;
        IReadOnlyList<InlineVm> title = admonition.Title is { } text ? [new RunVm(text) { Source = heading }] : [];
        return CalloutKinds.Callout(admonition.Kind, title, context.ConvertChildren(admonition), heading) with
        {
            IsFoldable = admonition.IsCollapsible,
            IsFolded = admonition.IsCollapsible && !admonition.IsExpanded
        };
    }
}
