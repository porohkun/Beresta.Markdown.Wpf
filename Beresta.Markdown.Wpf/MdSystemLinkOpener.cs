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

    private static readonly string[] MailFields = ["subject", "body", "cc", "bcc"];

    private MdSystemLinkOpener()
    {
    }

    /// <summary>The single instance of the opener.</summary>
    public static MdSystemLinkOpener Instance { get; } = new();

    /// <inheritdoc/>
    public void Open(string href, Uri? uri)
    {
        if (TargetOf(href) is not { } target)
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

    /// <summary>
    /// Адрес, который уходит системе: нормализованный адрес веба или почты, у почты в запросе — только
    /// тема, текст и копии. Не веб и не почта — <c>null</c>.
    /// </summary>
    internal static string? TargetOf(string href)
    {
        var text = href.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? "http://" + href : href;
        if (!Uri.TryCreate(text, UriKind.Absolute, out var absolute) || !ExternalSchemes.Contains(absolute.Scheme))
        {
            return null;
        }

        if (absolute.Scheme != Uri.UriSchemeMailto || absolute.Query.Length == 0)
        {
            return absolute.AbsoluteUri;
        }

        var fields = absolute.Query[1..]
            .Split('&')
            .Where(field => MailFields.Contains(field.Split('=')[0], StringComparer.OrdinalIgnoreCase))
            .ToList();

        var address = absolute.GetLeftPart(UriPartial.Path);
        return fields.Count == 0 ? address : $"{address}?{string.Join("&", fields)}";
    }
}
