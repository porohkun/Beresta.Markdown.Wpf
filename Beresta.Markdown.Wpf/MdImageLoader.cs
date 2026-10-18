namespace Beresta.Markdown.Wpf;

using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

/// <summary>
/// The default image loader: http(s), files, <c>pack://</c> and base64 <c>data:</c>. An image is
/// decoded outside the interface thread and returned frozen.
/// </summary>
public sealed class MdImageLoader : IMdImageLoader
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

    private MdImageLoader()
    {
    }

    /// <summary>The single instance of the loader.</summary>
    public static MdImageLoader Instance { get; } = new();

    /// <inheritdoc/>
    public Task<ImageSource?> LoadAsync(string url, Uri? uri, CancellationToken cancellationToken)
        => LoadAsync(url, uri, 0, cancellationToken);

    /// <summary>Картинка, декодированная не шире <paramref name="width"/> пикселей; 0 — в полный размер.</summary>
    internal async Task<ImageSource?> LoadAsync(string url, Uri? uri, int width, CancellationToken cancellationToken)
    {
        var bytes = await ReadAsync(url, uri, cancellationToken);
        return bytes is null ? null : await Task.Run(() => Decode(bytes, width), cancellationToken);
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

    private static ImageSource? Decode(byte[] bytes, int width)
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            if (width > 0 && PixelWidthOf(bytes) > width)
            {
                image.DecodePixelWidth = width;
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

    /// <summary>Ширина картинки в пикселях по её заголовку, без декодирования.</summary>
    private static int PixelWidthOf(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None).Frames[0].PixelWidth;
    }
}