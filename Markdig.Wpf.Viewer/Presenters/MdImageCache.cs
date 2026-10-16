namespace MarkdigWpfViewer.Presenters;

using System.Windows.Media;

/// <summary>
/// Загрузки картинок по загрузчику и адресу: блок, вернувшийся из-за края видимого, получает уже
/// загруженную картинку сразу. Помнит последние <see cref="Capacity"/> адресов; только поток интерфейса.
/// </summary>
internal static class MdImageCache
{
    private const int Capacity = 64;

    private static readonly Dictionary<(IMdImageLoader Loader, string Key), Task<ImageSource?>> Entries = [];
    private static readonly LinkedList<(IMdImageLoader Loader, string Key)> Recent = [];

    public static Task<ImageSource?> Get(IMdImageLoader loader, string key, Func<Task<ImageSource?>> load)
    {
        var id = (loader, key);
        if (Entries.TryGetValue(id, out var cached))
        {
            Recent.Remove(id);
            Recent.AddFirst(id);
            return cached;
        }

        var loading = load();
        Entries[id] = loading;
        Recent.AddFirst(id);

        if (Recent.Count > Capacity)
        {
            Entries.Remove(Recent.Last!.Value);
            Recent.RemoveLast();
        }

        return loading;
    }
}