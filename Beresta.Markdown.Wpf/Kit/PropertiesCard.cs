namespace Beresta.Markdown.Wpf.Kit;

using System.Windows;
using Extensions;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Helpers;
using Markdig.Syntax;
using ViewModels;

/// <summary>
/// The properties card: front matter at the start of the document is drawn as a «key — value»
/// table; the look is the theme template <c>Md.Properties.Template</c>. While controls edit the
/// source and <see cref="MarkdownViewer.IsFrontMatterEditable"/> is on, flat values and simple lists
/// are edited in input fields.
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
                .Select(property => new PropertyVm(KitLines.Runs(property.Key), KitLines.Runs(property.Value))
                {
                    Source = KitLines.SpanOf(lines[property.First], lines[property.Last]),
                    ValueText = property.Value,
                    ValueKind = property.Place?.Kind ?? MdValueKind.ReadOnly,
                    ValueSource = property.Place is { } place ? SpanOf(lines, place) : default,
                    Place = property.Place
                })
                .ToList());
    }

    /// <inheritdoc/>
    public override MdSourceEdit? EditSource(NodeVm node, object? state, MdSourceContext context)
        => node is PropertiesVm && state is PropertyEdit { Property: { Place: { } place } property } edit
            ? FrontMatterProperties.Write(place, property.ValueSource, edit.Text, context.Text)
            : null;

    /// <inheritdoc/>
    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item is PropertiesVm && container is FrameworkElement element ? element.TryFindResource("Md.Properties.Template") as DataTemplate : null;

    /// <summary>Место значения в исходнике: от двоеточия до конца значения.</summary>
    private static MdSourceSpan SpanOf(List<StringLine> lines, FrontMatterValue place)
    {
        var start = lines[place.Line].Slice.Start + place.Start;
        var end = lines[place.LastLine].Slice.Start + place.End;
        return new(start, Math.Max(0, end - start), lines[place.Line].Line, lines[place.LastLine].Line);
    }
}
