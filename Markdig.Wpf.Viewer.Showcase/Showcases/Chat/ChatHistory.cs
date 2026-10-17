namespace MarkdigWpfViewer.Showcase.Showcases.Chat;

using System.Globalization;
using System.Text;

/// <summary>Длинная история чата витрины, одна и та же при каждом запуске: короткие и длинные сообщения вперемешку.</summary>
public static class ChatHistory
{
    /// <summary>Каким по счёту с конца ленты стоит протокол встречи на несколько экранов.</summary>
    private const int MinutesFromEnd = 3;

    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

    private static readonly string[] Logins = ["me", "anna", "boris", "vera"];

    private static readonly string[] Replies =
    [
        "Ок",
        "Ага, понял :+1:",
        "Спасибо! :pray:",
        "Сборка зелёная :white_check_mark:",
        "@anna глянешь? :eyes:",
        "@boris созвон в три?",
        "@vera ревью готово",
        "Готово :tada:",
        "Не воспроизводится :thinking:",
        "Смержил :rocket:",
        "Кофе? :coffee:",
        "Через пять минут буду",
        "Огонь :fire:",
        "Ответ — ||42|| :wink:",
        "Нашёл баг :bug:",
        "Сейчас посмотрю",
        "Да",
        "Нет, давай завтра",
        ":joy::joy::joy:",
        ":heart:"
    ];

    private static readonly string[] Sentences =
    [
        "Перенёс **разбор** в отдельный метод, интерфейс больше не замирает.",
        "Проверь `MdBlockPanel`: созданные блоки теперь держатся, пока не уйдут далеко.",
        "@vera посмотри, пожалуйста, ревью — там пара вопросов по API.",
        "Документация лежит в [вики](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki).",
        "Кто брал ноутбук из переговорки? :sweat_smile:",
        "Спойлер к релизу: ||выходим в пятницу||.",
        "Тесты зелёные, *покрытие* выросло на пару процентов.",
        "Дизайн согласовали, осталось поправить отступы в пузырях.",
        "Нашёл утечку: подписка на `ScrollChanged` не снималась.",
        "Обновил зависимости, Markdig теперь последней версии.",
        "@anna, твой вариант с ленивой загрузкой картинок лучше :muscle:",
        "Длинные сообщения теперь создают только видимые абзацы.",
        "Прокрутка остаётся плавной даже на десяти тысячах сообщений :zap:",
        "Вечером выложу сборку, а утром соберём отзывы.",
        "@boris не забудь про ~~старый~~ новый формат настроек."
    ];

    private static readonly string[] Points =
    [
        "поправили виртуализацию ленты",
        "ускорили разбор длинных абзацев",
        "@anna добавила тёмную тему",
        "переписали копирование в markdown",
        "закрыли три бага с выделением",
        "@boris настроил ночные сборки",
        "добавили поиск по ленте"
    ];

    /// <summary>Участники по имени для упоминаний.</summary>
    public static IReadOnlyDictionary<string, string> People { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["me"] = "Я",
        ["anna"] = "Анна Смирнова",
        ["boris"] = "Борис Иванов",
        ["vera"] = "Вера Козлова"
    };

    /// <summary>История из <paramref name="count"/> сообщений по возрастанию времени.</summary>
    public static IReadOnlyList<ChatMessage> Generate(int count)
    {
        var random = new Random(7);
        var time = new DateTime(2026, 3, 2, 9, 0, 0);
        var messages = new List<ChatMessage>(count);

        for (var i = 0; i < count; i++)
        {
            time = time.AddMinutes(random.Next(1, 40));
            var stamp = time.ToString("d MMMM, HH:mm", Russian);
            if (i == count - MinutesFromEnd)
            {
                messages.Add(new(People["me"], stamp, Minutes(random, 40), true));
                continue;
            }

            var login = Logins[random.Next(Logins.Length)];
            var text = Text(random);
            while (text.Contains('@' + login, StringComparison.Ordinal))
            {
                text = Text(random);
            }

            messages.Add(new(People[login], stamp, text, login == "me"));
        }

        return messages;
    }

    private static string Text(Random random) => random.Next(100) switch
    {
        < 45 => Replies[random.Next(Replies.Length)],
        < 85 => Paragraph(random, 1 + random.Next(3)),
        < 97 => Summary(random),
        _ => Minutes(random, 20 + random.Next(30))
    };

    /// <summary>Абзац из нескольких фраз.</summary>
    private static string Paragraph(Random random, int sentences)
        => string.Join(" ", Enumerable.Range(0, sentences).Select(_ => Sentences[random.Next(Sentences.Length)]));

    /// <summary>Итоги: список, иногда код и цитата, абзац в конце.</summary>
    private static string Summary(Random random)
    {
        var text = new StringBuilder("Итоги дня:\n\n");
        foreach (var point in Points.OrderBy(_ => random.Next()).Take(2 + random.Next(4)))
        {
            text.Append("- ").Append(point).Append('\n');
        }

        if (random.Next(2) == 0)
        {
            text.Append("\n```csharp\nvar viewer = new MarkdownViewer { Markdown = message.Text };\n```\n");
        }

        if (random.Next(3) == 0)
        {
            text.Append("\n> Главное — не перерисовывать ленту целиком.\n");
        }

        return text.Append('\n').Append(Paragraph(random, 2)).ToString();
    }

    /// <summary>Протокол встречи: <paramref name="items"/> пунктов по абзацу, по шесть под заголовком.</summary>
    private static string Minutes(Random random, int items)
    {
        var text = new StringBuilder("**Протокол встречи**");
        for (var i = 1; i <= items; i++)
        {
            if (i % 6 == 1)
            {
                text.Append("\n\n### Вопрос ").Append(i / 6 + 1);
            }

            text.Append("\n\n**Пункт ").Append(i).Append(".** ").Append(Paragraph(random, 2 + random.Next(3)));
        }

        return text.ToString();
    }
}
