namespace Beresta.Markdown.Wpf.Abstractions;

using System.Windows;

/// <summary>
/// Templates of built-in blocks and inline elements, one per kind; <c>null</c> — the element is not
/// drawn.
/// </summary>
public interface IMdStyleProvider
{
    /// <summary>A paragraph.</summary>
    DataTemplate? ParagraphTemplate { get; }

    /// <summary>A heading.</summary>
    DataTemplate? HeadingTemplate { get; }

    /// <summary>A code block.</summary>
    DataTemplate? CodeBlockTemplate { get; }

    /// <summary>A quote.</summary>
    DataTemplate? QuoteTemplate { get; }

    /// <summary>A list.</summary>
    DataTemplate? ListTemplate { get; }

    /// <summary>A table.</summary>
    DataTemplate? TableTemplate { get; }

    /// <summary>A horizontal rule.</summary>
    DataTemplate? HorizontalRuleTemplate { get; }

    /// <summary>The space in place of blank source lines between blocks.</summary>
    DataTemplate? BlankLineTemplate { get; }

    /// <summary>A footnote definition.</summary>
    DataTemplate? FootnoteTemplate => null;

    /// <summary>An image, inline.</summary>
    DataTemplate? ImageTemplate => null;

    /// <summary>A task box, inline.</summary>
    DataTemplate? TaskTemplate => null;

    /// <summary>A footnote link, inline: a number or ↩.</summary>
    DataTemplate? FootnoteLinkTemplate => null;

    /// <summary>A figure, <c>^^^</c>.</summary>
    DataTemplate? FigureTemplate => null;

    /// <summary>The caption of a figure.</summary>
    DataTemplate? FigureCaptionTemplate => null;

    /// <summary>A footer, <c>^^</c>.</summary>
    DataTemplate? FooterTemplate => null;

    /// <summary>A callout.</summary>
    DataTemplate? CalloutTemplate => null;

    /// <summary>A collapsible block.</summary>
    DataTemplate? DetailsTemplate => null;

    /// <summary>A definition list.</summary>
    DataTemplate? DefinitionListTemplate => null;

    /// <summary>Blocks with a common alignment from HTML.</summary>
    DataTemplate? AlignedTemplate => null;
}