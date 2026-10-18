namespace Beresta.Markdown.Wpf.Kit;

/// <summary>
/// Parts of the sets of ready-made extensions by the names of the extensions, for
/// <see cref="KitSet.Without"/>.
/// </summary>
[Flags]
public enum KitParts
{
    /// <summary>No parts.</summary>
    None = 0,
    /// <summary><see cref="Kit.PropertiesCard"/>.</summary>
    PropertiesCard = 1,
    /// <summary><see cref="Kit.Mentions"/>.</summary>
    Mentions = 1 << 1,
    /// <summary><see cref="Kit.IssueLinks"/>.</summary>
    IssueLinks = 1 << 2,
    /// <summary><see cref="Kit.ColorChips"/>.</summary>
    ColorChips = 1 << 3,
    /// <summary><see cref="Kit.EmojiShortcodes"/>.</summary>
    EmojiShortcodes = 1 << 4,
    /// <summary><see cref="Kit.TableOfContents"/>.</summary>
    TableOfContents = 1 << 5,
    /// <summary><see cref="Kit.WikiLinks"/>.</summary>
    WikiLinks = 1 << 6,
    /// <summary><see cref="Kit.WikiEmbeds"/>.</summary>
    WikiEmbeds = 1 << 7,
    /// <summary><see cref="Kit.Tags"/>.</summary>
    Tags = 1 << 8,
    /// <summary><see cref="Kit.ObsidianCallouts"/>.</summary>
    ObsidianCallouts = 1 << 9,
    /// <summary><see cref="Kit.Comments"/>.</summary>
    Comments = 1 << 10,
    /// <summary><see cref="Kit.TaskStatuses"/>.</summary>
    TaskStatuses = 1 << 11,
    /// <summary><see cref="Kit.MkDocsAdmonitions"/>.</summary>
    MkDocsAdmonitions = 1 << 12,
    /// <summary><see cref="Kit.CalloutContainers"/>.</summary>
    CalloutContainers = 1 << 13,
    /// <summary><see cref="Kit.Shortcodes"/>.</summary>
    Shortcodes = 1 << 14,
    /// <summary><see cref="Kit.Spoilers"/>.</summary>
    Spoilers = 1 << 15
}
