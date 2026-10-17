namespace MarkdigWpfViewer.Search;

using System.Windows;

/// <summary>
/// Поиск в ленте: запрос на списке вьюеров — каждый созданный вьюер внутри подсвечивает свои
/// совпадения оформлением темы <c>Found</c>. Текущего совпадения и прокрутки нет.
/// </summary>
public static class MdFeedSearch
{
    /// <summary>Запрос; пустой — ничего не подсвечено. Наследуется.</summary>
    public static readonly DependencyProperty QueryProperty =
        DependencyProperty.RegisterAttached(
            "Query",
            typeof(string),
            typeof(MdFeedSearch),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

    /// <summary>Учитывать регистр. Наследуется.</summary>
    public static readonly DependencyProperty MatchCaseProperty =
        DependencyProperty.RegisterAttached(
            "MatchCase",
            typeof(bool),
            typeof(MdFeedSearch),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

    /// <summary>Искать слово целиком. Наследуется.</summary>
    public static readonly DependencyProperty WholeWordProperty =
        DependencyProperty.RegisterAttached(
            "WholeWord",
            typeof(bool),
            typeof(MdFeedSearch),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

    public static string? GetQuery(DependencyObject element)
        => (string?)element.GetValue(QueryProperty);

    public static void SetQuery(DependencyObject element, string? value)
        => element.SetValue(QueryProperty, value);

    public static bool GetMatchCase(DependencyObject element)
        => (bool)element.GetValue(MatchCaseProperty);

    public static void SetMatchCase(DependencyObject element, bool value)
        => element.SetValue(MatchCaseProperty, value);

    public static bool GetWholeWord(DependencyObject element)
        => (bool)element.GetValue(WholeWordProperty);

    public static void SetWholeWord(DependencyObject element, bool value)
        => element.SetValue(WholeWordProperty, value);
}
