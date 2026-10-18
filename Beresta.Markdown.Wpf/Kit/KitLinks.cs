namespace Beresta.Markdown.Wpf.Kit;

/// <summary>Цели связей готовых расширений: <c>wiki:</c>, <c>tag:</c>, <c>mention:</c> и адреса задач.</summary>
internal static class KitLinks
{
    /// <summary>Адрес для рисования ссылкой; цель с пробелами и другими знаками — экранированной.</summary>
    public static Uri? UriOf(string href)
        => Uri.TryCreate(href, UriKind.RelativeOrAbsolute, out var uri)
            ? uri
            : Uri.TryCreate(Uri.EscapeDataString(href), UriKind.Relative, out var escaped) ? escaped : null;
}
