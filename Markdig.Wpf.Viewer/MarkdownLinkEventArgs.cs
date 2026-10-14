namespace MarkdigWpfViewer;

using System.Windows;

/// <summary>
/// Нажатая ссылка. Цель отдаётся дважды: <see cref="Href"/> — как она записана в markdown,
/// <see cref="Uri"/> — её разбор.
///
/// <para>
/// Разбор годится не всякой ссылке: схема с подчёркиванием (<c>codex_page:...</c>) по RFC 3986
/// незаконна, и такой адрес становится относительным <see cref="System.Uri"/>. Обработчику
/// собственных ссылок поэтому нужен именно <see cref="Href"/> — исходная строка, ничем не
/// нормализованная.
/// </para>
/// </summary>
public sealed class MarkdownLinkEventArgs : RoutedEventArgs
{
    public MarkdownLinkEventArgs(RoutedEvent routedEvent, object source, string href, Uri? uri)
        : base(routedEvent, source)
    {
        Href = href;
        Uri = uri;
    }

    /// <summary>Цель ссылки ровно так, как она записана в markdown.</summary>
    public string Href { get; }

    /// <summary>Разбор <see cref="Href"/>; <c>null</c> — не разобралось вовсе.</summary>
    public Uri? Uri { get; }

    /// <inheritdoc />
    protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget) =>
        ((MarkdownLinkEventHandler)genericHandler)(genericTarget, this);
}
