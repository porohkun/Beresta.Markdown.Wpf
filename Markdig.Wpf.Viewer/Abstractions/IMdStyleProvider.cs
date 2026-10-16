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

    /// <summary>Фигура <c>^^^</c>.</summary>
    DataTemplate? FigureTemplate => null;

    /// <summary>Подпись фигуры.</summary>
    DataTemplate? FigureCaptionTemplate => null;

    /// <summary>Подвал <c>^^</c>.</summary>
    DataTemplate? FooterTemplate => null;

    /// <summary>Плашка.</summary>
    DataTemplate? CalloutTemplate => null;

    /// <summary>Сворачиваемый блок.</summary>
    DataTemplate? DetailsTemplate => null;

    /// <summary>Список определений.</summary>
    DataTemplate? DefinitionListTemplate => null;
}
