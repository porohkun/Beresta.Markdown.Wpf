namespace Beresta.Markdown.Wpf.Kit;

/// <summary>The GitHub set: properties card, mentions, issue links, color chips, emoji.</summary>
public sealed class GitHubSet : KitSet
{
    private readonly Mentions _mentions;
    private readonly IssueLinks _issues;

    /// <summary>Creates the set with all its parts.</summary>
    public GitHubSet()
    {
        Part(KitParts.PropertiesCard, new PropertiesCard());
        _mentions = Part(KitParts.Mentions, new Mentions());
        _issues = Part(KitParts.IssueLinks, new IssueLinks());
        Part(KitParts.ColorChips, new ColorChips());
        Part(KitParts.EmojiShortcodes, new EmojiShortcodes());
    }

    /// <summary>Knows the display names of people for mentions.</summary>
    public IMdLinkResolver? Resolver
    {
        get => _mentions.Resolver;
        set => _mentions.Resolver = value;
    }

    /// <summary>
    /// Templates of issue links: the prefix of an issue and an address with <c>{0}</c> in place of
    /// the number.
    /// </summary>
    public Dictionary<string, string> IssueTemplates => _issues.Templates;
}
