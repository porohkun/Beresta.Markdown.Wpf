namespace Beresta.Markdown.Wpf;

using Presenters;

/// <summary>
/// Loaded images shared by all viewers: a memory budget and dropping images so that the viewers
/// showing them load them again.
/// </summary>
public static class MdImageCache
{
    /// <summary>Сброшены картинки адреса; <c>null</c> — все. Приходит из того потока, где сбросили.</summary>
    internal static event Action<string?>? Invalidated;

    /// <summary>
    /// The memory budget in bytes; when loaded images take more, the ones not needed for longest are
    /// dropped. 128 MB by default.
    /// </summary>
    public static long Budget
    {
        get => ImageLoads.Shared.Budget;
        set => ImageLoads.Shared.Budget = value;
    }

    /// <summary>Drops every image: the viewers load the images they show again.</summary>
    public static void Clear()
    {
        ImageLoads.Shared.Clear();
        Invalidated?.Invoke(null);
    }

    /// <summary>
    /// Drops the image of an address — the way the viewer resolved it against
    /// <see cref="MarkdownViewer.BaseUri"/>: the viewers showing it load it again.
    /// </summary>
    public static void Invalidate(Uri address)
    {
        var key = address.ToString();
        ImageLoads.Shared.Invalidate(key);
        Invalidated?.Invoke(key);
    }
}
