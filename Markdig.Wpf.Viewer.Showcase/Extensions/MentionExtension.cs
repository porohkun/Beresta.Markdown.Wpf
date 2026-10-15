namespace MarkdigWpfViewer.Showcase.Extensions;

using System.Windows;
using Markdig;
using Markdig.Syntax.Inlines;
using MarkdigWpfViewer.Extensions;
using ViewModels;

/// <summary><c>@имя</c> — упоминание, свой синтаксис через свою настройку разбора.</summary>
public sealed class MentionExtension : MdExtension
{
    /// <summary>Шаблон упоминания; данные — <see cref="MentionVm"/>.</summary>
    public DataTemplate? Template { get; set; }

    public override void SetupPipeline(MarkdownPipelineBuilder pipeline)
        => pipeline.Extensions.AddIfNotAlready<MentionMarkdigExtension>();

    public override InlineVm? ConvertInline(Inline inline, MdConvertContext context)
        => inline is MentionInline mention ? new MentionVm(mention.Name) : null;

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item is MentionVm ? Template : null;
}
