namespace Beresta.Markdown.Wpf.Kit;

/// <summary>Набор GitHub: карточка свойств, упоминания, ссылки на задачи, цветовые чипы, эмодзи.</summary>
public sealed class GitHubSet : KitSet
{
    private readonly Mentions _mentions;
    private readonly IssueLinks _issues;

    public GitHubSet()
    {
        Part(KitParts.PropertiesCard, new PropertiesCard());
        _mentions = Part(KitParts.Mentions, new Mentions());
        _issues = Part(KitParts.IssueLinks, new IssueLinks());
        Part(KitParts.ColorChips, new ColorChips());
        Part(KitParts.EmojiShortcodes, new EmojiShortcodes());
    }

    /// <summary>Знает отображаемые имена участников для упоминаний.</summary>
    public IMdLinkResolver? Resolver
    {
        get => _mentions.Resolver;
        set => _mentions.Resolver = value;
    }

    /// <summary>Шаблоны ссылок на задачи: префикс задачи и адрес с <c>{0}</c> на месте номера.</summary>
    public Dictionary<string, string> IssueTemplates => _issues.Templates;
}
