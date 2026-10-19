namespace Beresta.Markdown.Wpf;

using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

/// <summary>
/// The default image loader: http(s), files, <c>pack://</c> and base64 <c>data:</c>. An image is
/// decoded outside the interface thread and returned frozen.
/// </summary>
public sealed class MdImageLoader : IMdImageLoader
{
    private static readonly HttpClient Http = CreateHttp();

    private MdImageLoader()
    {
    }

    /// <summary>The single instance of the loader.</summary>
    public static MdImageLoader Instance { get; } = new();

    /// <summary>
    /// The largest image in bytes the loader reads from the network, a file or <c>data:</c>; a larger
    /// one shows its alt text. 20 MB by default.
    /// </summary>
    public static long MaxDownloadBytes { get; set; } = 20L * 1024 * 1024;

    /// <inheritdoc/>
    public async Task<ImageSource?> LoadAsync(string url, Uri? uri, int decodeWidth, CancellationToken cancellationToken)
    {
        var bytes = await ReadAsync(url, uri, MaxDownloadBytes, cancellationToken);
        return bytes is null ? null : await Task.Run(() => Decode(bytes, decodeWidth), cancellationToken);
    }

    /// <summary>
    /// An image decoded from bytes no wider than <paramref name="decodeWidth"/> pixels — 0 means full
    /// size — and frozen; a format WPF does not decode gives <c>null</c>. For loaders of your own.
    /// </summary>
    public static ImageSource? Decode(byte[] bytes, int decodeWidth)
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            if (decodeWidth > 0 && PixelWidthOf(bytes) > decodeWidth)
            {
                image.DecodePixelWidth = decodeWidth;
            }

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

    private static HttpClient CreateHttp()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var version = typeof(MdImageLoader).Assembly.GetName().Version?.ToString() ?? "0";
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Beresta.Markdown.Wpf", version));
        return http;
    }

    private static async Task<byte[]?> ReadAsync(string url, Uri? uri, long limit, CancellationToken cancellationToken)
    {
        if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return FromData(url, limit);
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
                    return await DownloadAsync(uri, limit, cancellationToken);

                case "file":
                    return new FileInfo(uri.LocalPath).Length > limit
                        ? null
                        : await File.ReadAllBytesAsync(uri.LocalPath, cancellationToken);

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

    /// <summary>Байты по сети, не больше <paramref name="limit"/>; больше — <c>null</c>.</summary>
    private static async Task<byte[]?> DownloadAsync(Uri uri, long limit, CancellationToken cancellationToken)
    {
        using var response = await Http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > limit)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var bytes = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (bytes.Length + read > limit)
            {
                return null;
            }

            bytes.Write(chunk, 0, read);
        }

        return bytes.ToArray();
    }

    /// <summary>Байты из <c>data:</c>-адреса в base64, не больше <paramref name="limit"/>; другой вид не поддерживается.</summary>
    private static byte[]? FromData(string url, long limit)
    {
        var comma = url.IndexOf(',');
        if (comma < 0 || !url.AsSpan(0, comma).EndsWith(";base64", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if ((long)(url.Length - comma - 1) / 4 * 3 > limit)
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

    /// <summary>Ширина картинки в пикселях по её заголовку, без декодирования.</summary>
    private static int PixelWidthOf(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None).Frames[0].PixelWidth;
    }
}
