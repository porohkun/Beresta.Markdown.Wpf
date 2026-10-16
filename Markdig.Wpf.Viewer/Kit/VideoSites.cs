namespace MarkdigWpfViewer.Kit;

using System.Text.RegularExpressions;
using ViewModels;

/// <summary>Адреса видео YouTube, Vimeo и RuTube и превью к ним.</summary>
internal static partial class VideoSites
{
    /// <summary>Адрес видео одного из сайтов; не видео — <c>false</c>.</summary>
    public static bool IsVideo(string? url) => url is not null && Video().IsMatch(url);

    /// <summary>Превью YouTube по адресу видео; у других сайтов — <c>null</c>.</summary>
    public static string? ThumbnailOf(string url)
        => YouTube().Match(url) is { Success: true } match ? $"https://img.youtube.com/vi/{match.Groups["id"].Value}/hqdefault.jpg" : null;

    /// <summary>Карточка видео; превью из сети — только если <paramref name="loadThumbnails"/>.</summary>
    public static VideoCardVm Card(string href, string title, string? thumbnail, bool loadThumbnails)
    {
        var network = thumbnail is not null && (thumbnail.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || thumbnail.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
        var preview = thumbnail is not null && (loadThumbnails || !network) ? new ImageVm(thumbnail, title) : null;
        return new(href, title, preview);
    }

    [GeneratedRegex(@"^https?://(?:www\.|m\.)?(?:youtube\.com/(?:watch\?(?:.*&)?v=|shorts/|embed/|live/)|youtu\.be/)(?<id>[\w-]{6,})", RegexOptions.IgnoreCase)]
    private static partial Regex YouTube();

    [GeneratedRegex(@"^https?://(?:www\.|m\.)?(?:youtube\.com/(?:watch\?|shorts/|embed/|live/)|youtu\.be/|vimeo\.com/\d+|player\.vimeo\.com/video/\d+|rutube\.ru/(?:video|play/embed|shorts)/)", RegexOptions.IgnoreCase)]
    private static partial Regex Video();
}
