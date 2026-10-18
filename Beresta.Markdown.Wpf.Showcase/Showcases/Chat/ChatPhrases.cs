namespace Beresta.Markdown.Wpf.Showcase.Showcases.Chat;

/// <summary>Фразы истории чата на одном языке.</summary>
/// <param name="Replies">Короткие ответы.</param>
/// <param name="Sentences">Фразы, из которых собираются абзацы.</param>
/// <param name="Points">Пункты итогов дня.</param>
/// <param name="Summary">Заголовок итогов дня.</param>
/// <param name="Quote">Цитата в итогах.</param>
/// <param name="Minutes">Заголовок протокола встречи.</param>
/// <param name="Topic">Заголовок раздела протокола, перед номером.</param>
/// <param name="Item">Пункт протокола, перед номером.</param>
public sealed record ChatPhrases(
    string[] Replies,
    string[] Sentences,
    string[] Points,
    string Summary,
    string Quote,
    string Minutes,
    string Topic,
    string Item);
