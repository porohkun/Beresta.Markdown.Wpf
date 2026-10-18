namespace Beresta.Markdown.Wpf.Kit;

/// <summary>
/// The Obsidian set: properties card, Obsidian callouts, comments, embedded images, wiki links,
/// tags, task statuses.
/// </summary>
public sealed class ObsidianSet : KitSet
{
    private readonly WikiEmbeds _embeds;
    private readonly WikiLinks _links;

    /// <summary>Creates the set with all its parts.</summary>
    public ObsidianSet()
    {
        Part(KitParts.PropertiesCard, new PropertiesCard());
        Part(KitParts.ObsidianCallouts, new ObsidianCallouts());
        Part(KitParts.Comments, new Comments());
        _embeds = Part(KitParts.WikiEmbeds, new WikiEmbeds());
        _links = Part(KitParts.WikiLinks, new WikiLinks());
        Part(KitParts.Tags, new Tags());
        Part(KitParts.TaskStatuses, new TaskStatuses());
    }

    /// <summary>Knows which pages exist, for wiki links and embeds.</summary>
    public IMdLinkResolver? Resolver
    {
        get => _links.Resolver;
        set
        {
            _links.Resolver = value;
            _embeds.Resolver = value;
        }
    }
}
