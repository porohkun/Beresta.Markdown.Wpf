namespace Beresta.Markdown.Wpf.Kit;

/// <summary>Части наборов готовых расширений по именам расширений — для <see cref="KitSet.Without"/>.</summary>
[Flags]
public enum KitParts
{
    None = 0,
    PropertiesCard = 1,
    Mentions = 1 << 1,
    IssueLinks = 1 << 2,
    ColorChips = 1 << 3,
    EmojiShortcodes = 1 << 4,
    TableOfContents = 1 << 5,
    WikiLinks = 1 << 6,
    WikiEmbeds = 1 << 7,
    Tags = 1 << 8,
    ObsidianCallouts = 1 << 9,
    Comments = 1 << 10,
    TaskStatuses = 1 << 11,
    MkDocsAdmonitions = 1 << 12,
    CalloutContainers = 1 << 13,
    Shortcodes = 1 << 14,
    Spoilers = 1 << 15
}
