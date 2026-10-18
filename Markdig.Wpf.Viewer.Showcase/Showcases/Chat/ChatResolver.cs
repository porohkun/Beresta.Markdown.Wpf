namespace MarkdigWpfViewer.Showcase.Showcases.Chat;

using MarkdigWpfViewer.Kit;

/// <summary>Участники чата витрины: имена для упоминаний.</summary>
public sealed class ChatResolver : IMdLinkResolver
{
    public static ChatResolver Instance { get; } = new();

    public bool PageExists(string page) => true;

    public string? DisplayName(string user)
        => ChatHistory.Logins.FirstOrDefault(login => string.Equals(login, user, StringComparison.OrdinalIgnoreCase)) is { } known
            ? ChatHistory.NameOf(known)
            : null;
}
