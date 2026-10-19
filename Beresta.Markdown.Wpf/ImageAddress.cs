namespace Beresta.Markdown.Wpf;

/// <summary>Адрес картинки из разметки, разобранный от адреса документа.</summary>
internal static class ImageAddress
{
    /// <summary>Абсолютный адрес, от адреса документа или относительный; у <c>data:</c> и неразборчивого — <c>null</c>.</summary>
    public static Uri? Resolve(string url, Uri? baseUri)
    {
        if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute))
        {
            return absolute;
        }

        if (baseUri is { IsAbsoluteUri: true } && Uri.TryCreate(baseUri, url, out var combined))
        {
            return combined;
        }

        return Uri.TryCreate(url, UriKind.Relative, out var relative) ? relative : null;
    }
}
