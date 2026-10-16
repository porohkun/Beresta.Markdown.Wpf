namespace MarkdigWpfViewer.Kit;

/// <summary>Набор GitLab: оглавление, ссылки на задачи, цветовые чипы, эмодзи.</summary>
public sealed class GitLabSet : KitSet
{
    private readonly IssueLinks _issues;

    public GitLabSet()
    {
        Part(KitParts.TableOfContents, new TableOfContents());
        _issues = Part(KitParts.IssueLinks, new IssueLinks());
        Part(KitParts.ColorChips, new ColorChips());
        Part(KitParts.EmojiShortcodes, new EmojiShortcodes());
    }

    /// <summary>Шаблоны ссылок на задачи: префикс задачи и адрес с <c>{0}</c> на месте номера.</summary>
    public Dictionary<string, string> IssueTemplates => _issues.Templates;
}
