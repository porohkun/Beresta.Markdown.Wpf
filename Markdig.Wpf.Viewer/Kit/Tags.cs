namespace MarkdigWpfViewer.Kit;

using Extensions;
using Markdig;
using Markdig.Syntax.Inlines;
using ViewModels;

/// <summary>
/// Теги <c>#тег</c> и <c>#тег/вложенный</c> — чипом с оформлением <c>Tag</c> и ссылкой <c>tag:тег</c>.
/// Решётка внутри ссылки — текст ссылки.
/// </summary>
public sealed class Tags : MdExtension
{
    public override void SetupPipeline(MarkdownPipelineBuilder pipeline)
        => pipeline.Extensions.AddIfNotAlready<TagMarkdigExtension>();

    public override IReadOnlyList<InlineVm>? ConvertInlines(Inline inline, MdConvertContext context)
    {
        if (inline is not TagInline tag)
        {
            return null;
        }

        var run = context.CreateRun("#" + tag.Name);
        if (run.Href is not null)
        {
            return [run];
        }

        var href = "tag:" + tag.Name;
        return
        [
            run with
            {
                Href = href,
                Hyperlink = KitLinks.UriOf(href),
                StyleName = "Tag",
                Markup = new(string.Empty, string.Empty) { ReplacesLink = true }
            }
        ];
    }
}
