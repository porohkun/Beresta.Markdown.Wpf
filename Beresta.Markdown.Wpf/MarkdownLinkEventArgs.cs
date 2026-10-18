namespace Beresta.Markdown.Wpf;

using System.Windows;

/// <summary>
/// A clicked link. The target comes twice: <see cref="Href"/> as written in the Markdown,
/// <see cref="Uri"/> parsed.
/// </summary>
/// <remarks>
/// Not every target parses: a scheme with an underscore, <c>codex_page:...</c>, is illegal by RFC
/// 3986 and becomes a relative <see cref="System.Uri"/>. A handler of your own links needs
/// <see cref="Href"/>, the original string.
/// </remarks>
public sealed class MarkdownLinkEventArgs : RoutedEventArgs
{
    /// <summary>A clicked link with its target as written and parsed.</summary>
    public MarkdownLinkEventArgs(RoutedEvent routedEvent, object source, string href, Uri? uri)
        : base(routedEvent, source)
    {
        Href = href;
        Uri = uri;
    }

    /// <summary>The target of the link exactly as written in the Markdown.</summary>
    public string Href { get; }

    /// <summary>The parsed <see cref="Href"/>; <c>null</c> — it did not parse at all.</summary>
    public Uri? Uri { get; }

    /// <inheritdoc/>
    protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget) =>
        ((MarkdownLinkEventHandler)genericHandler)(genericTarget, this);
}