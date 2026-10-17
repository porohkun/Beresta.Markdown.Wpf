namespace MarkdigWpfViewer.Showcase.Showcases.Chat;

/// <summary>Чат на десять тысяч сообщений: вьюер в каждом пузыре, создаются только видимые.</summary>
public sealed class ChatViewModel : ShowcaseViewModel
{
    public ChatViewModel(ShowcaseOptions options)
        : base(options)
    {
    }

    public override string Title => "Чат";

    public IReadOnlyList<ChatMessage> Messages => field ??= ChatHistory.Generate(10_000);
}
