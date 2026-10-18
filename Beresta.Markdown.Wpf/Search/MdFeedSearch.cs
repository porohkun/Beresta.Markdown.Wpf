namespace Beresta.Markdown.Wpf.Search;

using System.Windows;

/// <summary>
/// Search in a feed: a query on a list of viewers — every created viewer inside highlights its
/// matches with the theme style <c>Found</c>. No current match and no scrolling.
/// </summary>
public static class MdFeedSearch
{
    /// <summary>The query; empty — nothing is highlighted. Inherited.</summary>
    public static readonly DependencyProperty QueryProperty =
        DependencyProperty.RegisterAttached(
            "Query",
            typeof(string),
            typeof(MdFeedSearch),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

    /// <summary>Match case. Inherited.</summary>
    public static readonly DependencyProperty MatchCaseProperty =
        DependencyProperty.RegisterAttached(
            "MatchCase",
            typeof(bool),
            typeof(MdFeedSearch),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

    /// <summary>Match whole words. Inherited.</summary>
    public static readonly DependencyProperty WholeWordProperty =
        DependencyProperty.RegisterAttached(
            "WholeWord",
            typeof(bool),
            typeof(MdFeedSearch),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

    /// <inheritdoc cref="QueryProperty"/>
    public static string? GetQuery(DependencyObject element)
        => (string?)element.GetValue(QueryProperty);

    /// <inheritdoc cref="QueryProperty"/>
    public static void SetQuery(DependencyObject element, string? value)
        => element.SetValue(QueryProperty, value);

    /// <inheritdoc cref="MatchCaseProperty"/>
    public static bool GetMatchCase(DependencyObject element)
        => (bool)element.GetValue(MatchCaseProperty);

    /// <inheritdoc cref="MatchCaseProperty"/>
    public static void SetMatchCase(DependencyObject element, bool value)
        => element.SetValue(MatchCaseProperty, value);

    /// <inheritdoc cref="WholeWordProperty"/>
    public static bool GetWholeWord(DependencyObject element)
        => (bool)element.GetValue(WholeWordProperty);

    /// <inheritdoc cref="WholeWordProperty"/>
    public static void SetWholeWord(DependencyObject element, bool value)
        => element.SetValue(WholeWordProperty, value);
}
