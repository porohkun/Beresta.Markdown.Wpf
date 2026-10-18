namespace Beresta.Markdown.Wpf.Kit;

using ViewModels;

/// <summary>The HackMD set: containers <c>:::kind</c>, table of contents, shortcodes <c>{%youtube id %}</c>.</summary>
public sealed class HackMDSet : KitSet
{
    private readonly Shortcodes _shortcodes;

    /// <summary>Creates the set with all its parts.</summary>
    public HackMDSet()
    {
        Part(KitParts.CalloutContainers, new CalloutContainers());
        Part(KitParts.TableOfContents, new TableOfContents());
        _shortcodes = Part(KitParts.Shortcodes, new Shortcodes());
    }

    /// <summary>Shortcode handlers by name; the application adds its own names here.</summary>
    public Dictionary<string, Func<MdShortcode, NodeVm?>> ShortcodeHandlers => _shortcodes.Handlers;

    /// <summary>Load video previews from the network; when off, a card has no preview.</summary>
    public bool LoadThumbnails
    {
        get => _shortcodes.LoadThumbnails;
        set => _shortcodes.LoadThumbnails = value;
    }
}
