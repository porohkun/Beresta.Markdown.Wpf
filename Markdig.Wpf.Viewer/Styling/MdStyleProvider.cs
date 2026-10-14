namespace MarkdigWpfViewer.Styling;

using System.Windows;
using Abstractions;

internal sealed class MdStyleProvider : IMdStyleProvider
{
    public required DataTemplate ParagraphTemplate { get; set; }

    public required DataTemplate HeadingTemplate { get; set; }

    public required DataTemplate CodeBlockTemplate { get; set; }

    public required DataTemplate QuoteTemplate { get; set; }

    public required DataTemplate ListTemplate { get; set; }

    public required DataTemplate TableTemplate { get; set; }

    public required DataTemplate HorizontalRuleTemplate { get; set; }

    public required DataTemplate BlankLineTemplate { get; set; }
}