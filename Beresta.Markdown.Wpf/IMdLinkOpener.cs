namespace Beresta.Markdown.Wpf;

/// <summary>
/// Opens a link that the application did not handle; set in
/// <see cref="MarkdownLink.OpenerProperty"/>.
/// </summary>
public interface IMdLinkOpener
{
    /// <summary>
    /// Opens a link; <paramref name="href"/> is the target exactly as in the Markdown,
    /// <paramref name="uri"/> is its parsed form.
    /// </summary>
    void Open(string href, Uri? uri);
}