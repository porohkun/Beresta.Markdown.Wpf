namespace Beresta.Markdown.Wpf.Kit;

/// <summary>Набор чата — Slack, Discord, Telegram: упоминания, спойлеры, эмодзи.</summary>
public sealed class ChatSet : KitSet
{
    private readonly Mentions _mentions;

    public ChatSet()
    {
        _mentions = Part(KitParts.Mentions, new Mentions());
        Part(KitParts.Spoilers, new Spoilers());
        Part(KitParts.EmojiShortcodes, new EmojiShortcodes());
    }

    /// <summary>Знает отображаемые имена участников для упоминаний.</summary>
    public IMdLinkResolver? Resolver
    {
        get => _mentions.Resolver;
        set => _mentions.Resolver = value;
    }
}
