namespace MarkdigWpfViewer;

using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

/// <summary>
/// Загрузчик картинок по умолчанию: http(s), файлы, <c>pack://</c> и <c>data:</c> в base64.
/// Картинка декодируется вне потока интерфейса и отдаётся замороженной.
/// </summary>
public sealed class MdImageLoader : IMdImageLoader
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

    private MdImageLoader()
    {
    }

    public static MdImageLoader Instance { get; } = new();

    public async Task<ImageSource?> LoadAsync(string url, Uri? uri, CancellationToken cancellationToken)
    {
        var bytes = await ReadAsync(url, uri, cancellationToken);
        return bytes is null ? null : await Task.Run(() => Decode(bytes), cancellationToken);
    }

    private static async Task<byte[]?> ReadAsync(string url, Uri? uri, CancellationToken cancellationToken)
    {
        if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return FromData(url);
        }

        if (uri is not { IsAbsoluteUri: true })
        {
            return null;
        }

        try
        {
            switch (uri.Scheme)
            {
                case "http" or "https":
                    return await Http.GetByteArrayAsync(uri, cancellationToken);

                case "file":
                    return await File.ReadAllBytesAsync(uri.LocalPath, cancellationToken);

                case "pack":
                    await using (var stream = (Application.GetResourceStream(uri) ?? Application.GetContentStream(uri))?.Stream)
                    {
                        if (stream is null)
                        {
                            return null;
                        }

                        using var copy = new MemoryStream();
                        await stream.CopyToAsync(copy, cancellationToken);
                        return copy.ToArray();
                    }

                default:
                    return null;
            }
        }
        catch (Exception exception) when (exception
                                              is HttpRequestException
                                              or IOException
                                              or UnauthorizedAccessException
                                              or TaskCanceledException)
        {
            // BYDESIGN: недоступная картинка — обычное дело для документа; вместо неё виден alt.
            return null;
        }
    }

    /// <summary>Байты из <c>data:</c>-адреса в base64; другой вид не поддерживается.</summary>
    private static byte[]? FromData(string url)
    {
        var comma = url.IndexOf(',');
        if (comma < 0 || !url.AsSpan(0, comma).EndsWith(";base64", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            return Convert.FromBase64String(url[(comma + 1)..]);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static ImageSource? Decode(byte[] bytes)
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = new MemoryStream(bytes);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception exception) when (exception
                                              is NotSupportedException
                                              or FileFormatException
                                              or ArgumentException
                                              or InvalidOperationException)
        {
            // BYDESIGN: формат, который WPF не декодирует (SVG и подобные), показывается alt.
            return null;
        }
    }
}