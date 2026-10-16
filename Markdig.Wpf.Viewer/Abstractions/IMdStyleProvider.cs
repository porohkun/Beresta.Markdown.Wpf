namespace MarkdigWpfViewer.Abstractions;

using System.Windows;

/// <summary>Шаблоны встроенных блоков и строчных элементов, по одному на вид; <c>null</c> — элемент не рисуется.</summary>
public interface IMdStyleProvider
{
    DataTemplate? ParagraphTemplate { get; }

    DataTemplate? HeadingTemplate { get; }

    DataTemplate? CodeBlockTemplate { get; }

    DataTemplate? QuoteTemplate { get; }

    DataTemplate? ListTemplate { get; }

    DataTemplate? TableTemplate { get; }

    DataTemplate? HorizontalRuleTemplate { get; }

    DataTemplate? BlankLineTemplate { get; }

    /// <summary>Определение сноски.</summary>
    DataTemplate? FootnoteTemplate => null;

    /// <summary>Картинка, строчная.</summary>
    DataTemplate? ImageTemplate => null;

    /// <summary>Флажок задачи, строчный.</summary>
    DataTemplate? TaskTemplate => null;

    /// <summary>Ссылка сноски, строчная: номер или ↩.</summary>
    DataTemplate? FootnoteLinkTemplate => null;
}
