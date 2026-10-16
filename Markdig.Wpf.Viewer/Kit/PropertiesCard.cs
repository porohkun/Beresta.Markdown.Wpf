namespace MarkdigWpfViewer.Kit;

using System.Windows;
using Extensions;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using ViewModels;

/// <summary>
/// Карточка свойств: front matter в начале документа рисуется таблицей «ключ — значение». Разбор —
/// только для показа; вид — шаблон темы <c>Md.Properties.Template</c>.
/// </summary>
public sealed class PropertiesCard : MdExtension
{
    public override void SetupPipeline(MarkdownPipelineBuilder pipeline)
        => pipeline.UseYamlFrontMatter();

    public override BlockVm? ConvertBlock(Block block, MdConvertContext context)
    {
        if (block is not YamlFrontMatterBlock yaml)
        {
            return null;
        }

        var lines = KitLines.Of(yaml.Lines);
        var properties = FrontMatterProperties.Read(lines.Select(line => line.ToString()).ToList());
        return properties.Count == 0
            ? null
            : new PropertiesVm(properties
                .Select(property => new PropertyVm(KitLines.Runs(property.Key), KitLines.Runs(property.Value)) { Source = KitLines.SpanOf(lines[property.First], lines[property.Last]) })
                .ToList());
    }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item is PropertiesVm && container is FrameworkElement element ? element.TryFindResource("Md.Properties.Template") as DataTemplate : null;
}
