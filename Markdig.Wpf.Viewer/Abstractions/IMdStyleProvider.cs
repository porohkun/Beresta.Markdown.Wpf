namespace MarkdigWpfViewer.Abstractions;

using System.Windows;

public interface IMdStyleProvider
{
    DataTemplate ParagraphTemplate { get; }

    DataTemplate HeadingTemplate { get; }

    DataTemplate CodeBlockTemplate { get; }

    DataTemplate QuoteTemplate { get; }

    DataTemplate ListTemplate { get; }

    DataTemplate TableTemplate { get; }

    DataTemplate HorizontalRuleTemplate { get; }

    DataTemplate BlankLineTemplate { get; }
}