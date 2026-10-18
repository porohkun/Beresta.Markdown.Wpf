namespace Beresta.Markdown.Wpf.Kit;

/// <summary>The chat set — Slack, Discord, Telegram: mentions, spoilers, emoji.</summary>
public sealed class ChatSet : KitSet
{
    private readonly Mentions _mentions;

    /// <summary>Creates the set with all its parts.</summary>
    public ChatSet()
    {
        _mentions = Part(KitParts.Mentions, new Mentions());
        Part(KitParts.Spoilers, new Spoilers());
        Part(KitParts.EmojiShortcodes, new EmojiShortcodes());
    }

    /// <summary>Knows the display names of people for mentions.</summary>
    public IMdLinkResolver? Resolver
    {
        get => _mentions.Resolver;
        set => _mentions.Resolver = value;
    }
}
