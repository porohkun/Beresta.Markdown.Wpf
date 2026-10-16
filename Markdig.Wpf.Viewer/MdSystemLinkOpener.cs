namespace MarkdigWpfViewer;

using System.ComponentModel;
using System.Diagnostics;

/// <summary>
/// Открыватель по умолчанию: адреса веба и почты — системным обработчиком, остальное не открывает.
/// <c>www.example.com</c> без схемы считается адресом веба.
/// </summary>
public sealed class MdSystemLinkOpener : IMdLinkOpener
{
    private static readonly string[] ExternalSchemes = [Uri.UriSchemeHttp, Uri.UriSchemeHttps, Uri.UriSchemeMailto];

    private MdSystemLinkOpener()
    {
    }

    public static MdSystemLinkOpener Instance { get; } = new();

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