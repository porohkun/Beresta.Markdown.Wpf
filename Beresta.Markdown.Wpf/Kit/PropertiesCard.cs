namespace Beresta.Markdown.Wpf.Kit;

using System.Windows;
using Extensions;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using ViewModels;

/// <summary>
/// The properties card: front matter at the start of the document is drawn as a «key — value»
/// table. Parsed only for showing; the look is the theme template <c>Md.Properties.Template</c>.
/// </summary>
public sealed class PropertiesCard : MdExtension
{
    /// <inheritdoc/>
    public override void SetupPipeline(MarkdownPipelineBuilder pipeline)
        => pipeline.UseYamlFrontMatter();

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item is PropertiesVm && container is FrameworkElement element ? element.TryFindResource("Md.Properties.Template") as DataTemplate : null;
}
