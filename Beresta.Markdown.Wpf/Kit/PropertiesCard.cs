namespace Beresta.Markdown.Wpf.Kit;

using System.Collections.ObjectModel;
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
/// source and <see cref="MarkdownViewer.IsFrontMatterEditable"/> is on, values are edited by their
/// type, and properties are added and removed.
/// </summary>
public sealed class PropertiesCard : MdExtension
{
    /// <summary>
    /// The properties the application knows: their types are stronger than the guess by the value, and
    /// their keys are suggested by the row that adds a property.
    /// </summary>
    public Collection<MdPropertyField> Fields { get; } = [];

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
        var items = properties
            .Select(property => Property(property.Key, property.Value, property.Place) with
            {
                Source = KitLines.SpanOf(lines[property.First], lines[property.Last]),
                ValueSource = property.Place is { } place ? SpanOf(lines, place) : default
            })
            .ToList();
        var keys = properties.Select(property => property.Key).ToHashSet();
        return new PropertiesVm(items)
        {
            Suggestions = Fields
                .Select(field => field.Key)
                .Where(key => key.Length > 0 && !keys.Contains(key))
                .Distinct()
                .ToList()
        };
    }

    /// <inheritdoc/>
    public override MdSourceEdit? EditSource(NodeVm node, object? state, MdSourceContext context)
        => (node, state) switch
        {
            (PropertiesVm, PropertyEdit { Property: { Place: { } place } property } edit)
                => FrontMatterProperties.Write(WrittenAs(property, place), property.ValueSource, edit.Text, context.Text),
            (PropertiesVm, PropertyRemoval { Property.Source: { IsEmpty: false } span }) => FrontMatterProperties.Remove(span, context.Text),
            (PropertiesVm { Source: { IsEmpty: false } span } card, PropertyAddition { Key: var key })
                when card.Items.All(item => item.KeyText != key.Trim()) => FrontMatterProperties.Add(span, key, context.Text),
            _ => null
        };

    /// <inheritdoc/>
    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item is PropertiesVm && container is FrameworkElement element ? element.TryFindResource("Md.Properties.Template") as DataTemplate : null;

    /// <summary>
    /// Как записать новое значение: дату — простым значением без кавычек, список на месте простого
    /// значения — в скобках.
    /// </summary>
    private static FrontMatterValue WrittenAs(PropertyVm property, FrontMatterValue place)
        => property.Type == MdPropertyType.Date ? place with { Kind = MdValueKind.Plain }
            : property.Type == MdPropertyType.List && place.Kind is not (MdValueKind.FlowList or MdValueKind.BlockList) ? place with { Kind = MdValueKind.FlowList }
            : place;

    /// <summary>Строка карточки: значение по типу поля приложения, а без поля — по догадке по значению.</summary>
    private PropertyVm Property(string key, string value, FrontMatterValue? place)
    {
        var type = place is null ? MdPropertyType.Text : Fields.LastOrDefault(field => field.Key == key)?.Type ?? FrontMatterProperties.TypeOf(place, value);
        return new PropertyVm(KitLines.Runs(key), KitLines.Runs(value))
        {
            ValueText = value,
            ValueKind = place?.Kind ?? MdValueKind.ReadOnly,
            Place = place,
            KeyText = key,
            Type = type,
            Items = type == MdPropertyType.List ? FrontMatterProperties.Elements(value) : [],
            IsChecked = type == MdPropertyType.Bool && FrontMatterProperties.IsOn(value),
            Date = type == MdPropertyType.Date ? FrontMatterProperties.DateOf(value) : null
        };
    }

    /// <summary>Место значения в исходнике: от двоеточия до конца значения.</summary>
    private static MdSourceSpan SpanOf(List<StringLine> lines, FrontMatterValue place)
    {
        var start = lines[place.Line].Slice.Start + place.Start;
        var end = lines[place.LastLine].Slice.Start + place.End;
        return new(start, Math.Max(0, end - start), lines[place.Line].Line, lines[place.LastLine].Line);
    }
}
