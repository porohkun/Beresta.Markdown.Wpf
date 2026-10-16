namespace MarkdigWpfViewer.Kit;

using System.Windows;
using Extensions;
using Markdig;
using Markdig.Syntax.Inlines;
using ViewModels;

/// <summary>
/// Упоминания <c>@имя</c> — чипом со ссылкой <c>mention:имя</c>; вид — шаблон темы <c>Md.Mention.Template</c>.
/// Отображаемое имя даёт <see cref="Resolver"/>. <c>@</c> внутри ссылки — текст ссылки.
/// </summary>
public sealed class Mentions : MdExtension
{
    /// <summary>Знает отображаемые имена участников; нет — имя как написано.</summary>
    public IMdLinkResolver? Resolver { get; set; }

    public override void SetupPipeline(MarkdownPipelineBuilder pipeline)
        => pipeline.Extensions.AddIfNotAlready<MentionMarkdigExtension>();

    public override IReadOnlyList<InlineVm>? ConvertInlines(Inline inline, MdConvertContext context)
    {
        if (inline is not MentionInline mention)
        {
            return null;
        }

        var run = context.CreateRun("@" + mention.Name);
        return run.Href is not null
            ? [run]
            : [new MentionVm(mention.Name, Resolver?.DisplayName(mention.Name) ?? mention.Name)];
    }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item is MentionVm && container is FrameworkElement element ? element.TryFindResource("Md.Mention.Template") as DataTemplate : null;
}
