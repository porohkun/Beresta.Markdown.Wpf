namespace MarkdigWpfViewer;

/// <summary>Открывает ссылку, которую не обработало приложение; ставится в <see cref="MarkdownLink.OpenerProperty"/>.</summary>
public interface IMdLinkOpener
{
    /// <summary>Открывает ссылку; <paramref name="href"/> — цель ровно из markdown, <paramref name="uri"/> — её разбор.</summary>
    void Open(string href, Uri? uri);
}