namespace Beresta.Markdown.Wpf.Kit;

/// <summary>The GitLab set: table of contents, issue links, color chips, emoji.</summary>
public sealed class GitLabSet : KitSet
{
    private readonly IssueLinks _issues;

    /// <summary>Creates the set with all its parts.</summary>
    public GitLabSet()
    {
        Part(KitParts.TableOfContents, new TableOfContents());
        _issues = Part(KitParts.IssueLinks, new IssueLinks());
        Part(KitParts.ColorChips, new ColorChips());
        Part(KitParts.EmojiShortcodes, new EmojiShortcodes());
    }

    /// <summary>
    /// Templates of issue links: the prefix of an issue and an address with <c>{0}</c> in place of
    /// the number.
    /// </summary>
    public Dictionary<string, string> IssueTemplates => _issues.Templates;
}
