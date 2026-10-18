namespace Beresta.Markdown.Wpf.Showcase.Showcases.Chat;

/// <summary>Чат на десять тысяч сообщений: вьюер в каждом пузыре, создаются только видимые.</summary>
public sealed class ChatViewModel : ShowcaseViewModel
{
    private IReadOnlyList<ChatMessage>? _messages;

    public ChatViewModel(ShowcaseOptions options)
        : base(options)
    {
    }

    public IReadOnlyList<ChatMessage> Messages => _messages ??= ChatHistory.Generate(10_000);

    /// <summary>Язык сменился: история пишется заново на новом языке.</summary>
    protected override void OnLanguageChanged()
    {
        _messages = null;
        base.OnLanguageChanged();
    }
}
