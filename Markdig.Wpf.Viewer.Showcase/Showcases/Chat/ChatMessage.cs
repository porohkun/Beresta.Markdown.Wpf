namespace MarkdigWpfViewer.Showcase.Showcases.Chat;

/// <summary>Сообщение чата: автор, время и текст в markdown; своё сообщение стоит справа.</summary>
public sealed record ChatMessage(string Author, string Time, string Text, bool IsMine);
