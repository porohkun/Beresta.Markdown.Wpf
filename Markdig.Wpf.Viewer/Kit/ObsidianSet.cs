namespace MarkdigWpfViewer.Kit;

/// <summary>
/// Набор Obsidian: карточка свойств, плашки Obsidian, комментарии, встраивание картинок, вики-ссылки,
/// теги, статусы задач.
/// </summary>
public sealed class ObsidianSet : KitSet
{
    private readonly WikiEmbeds _embeds;
    private readonly WikiLinks _links;

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

    /// <summary>Знает, какие страницы есть, — для вики-ссылок и встраиваний.</summary>
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
