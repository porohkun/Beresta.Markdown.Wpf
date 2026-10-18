namespace Beresta.Markdown.Wpf;

using System.ComponentModel;
using System.Diagnostics;

/// <summary>
/// The default opener: addresses of the web and mail go to the system handler, the rest is not
/// opened. <c>www.example.com</c> without a scheme counts as a web address.
/// </summary>
public sealed class MdSystemLinkOpener : IMdLinkOpener
{
    private static readonly string[] ExternalSchemes = [Uri.UriSchemeHttp, Uri.UriSchemeHttps, Uri.UriSchemeMailto];

    private MdSystemLinkOpener()
    {
    }

    /// <summary>The single instance of the opener.</summary>
    public static MdSystemLinkOpener Instance { get; } = new();

    /// <inheritdoc/>
    public void Open(string href, Uri? uri)
    {
        var target = href.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? "http://" + href : href;
        if (!Uri.TryCreate(target, UriKind.Absolute, out var absolute) || !ExternalSchemes.Contains(absolute.Scheme))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            // BYDESIGN: адрес некому открыть — в системе нет обработчика; ссылка просто не открывается.
        }
    }
}