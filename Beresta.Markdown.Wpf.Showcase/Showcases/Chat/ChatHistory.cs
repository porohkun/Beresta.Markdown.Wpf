namespace Beresta.Markdown.Wpf.Showcase.Showcases.Chat;

using System.Globalization;
using System.Text;
using Localization;

/// <summary>Длинная история чата витрины, одна и та же при каждом запуске: короткие и длинные сообщения вперемешку.</summary>
public static class ChatHistory
{
    /// <summary>Каким по счёту с конца ленты стоит протокол встречи на несколько экранов.</summary>
    private const int MinutesFromEnd = 3;

    private static readonly ChatPhrases English = new(
        Replies:
        [
            "OK",
            "Got it :+1:",
            "Thanks! :pray:",
            "The build is green :white_check_mark:",
            "@anna can you take a look? :eyes:",
            "@boris a call at three?",
            "@vera the review is done",
            "Done :tada:",
            "Can't reproduce :thinking:",
            "Merged :rocket:",
            "Coffee? :coffee:",
            "Be there in five minutes",
            "Awesome :fire:",
            "The answer is ||42|| :wink:",
            "Found a bug :bug:",
            "Looking now",
            "Yes",
            "No, let's do it tomorrow",
            ":joy::joy::joy:",
            ":heart:"
        ],
        Sentences:
        [
            "Moved the **parsing** into a method of its own, the UI no longer freezes.",
            "Check `MdBlockPanel`: created blocks now stay until they go far away.",
            "@vera please look at the review — there are a couple of questions about the API.",
            "The documentation is in the [wiki](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki).",
            "Who took the laptop from the meeting room? :sweat_smile:",
            "A spoiler about the release: ||we ship on Friday||.",
            "Tests are green, *coverage* went up a couple of percent.",
            "The design is approved, only the paddings in the bubbles are left.",
            "Found a leak: the `ScrollChanged` subscription was never removed.",
            "Updated the dependencies, Markdig is at its latest version now.",
            "@anna, your take on lazy image loading is better :muscle:",
            "Long messages now create only the visible paragraphs.",
            "Scrolling stays smooth even with ten thousand messages :zap:",
            "I'll publish a build tonight, and we'll gather feedback in the morning.",
            "@boris don't forget the ~~old~~ new settings format."
        ],
        Points:
        [
            "fixed the feed virtualization",
            "sped up parsing of long paragraphs",
            "@anna added the dark theme",
            "rewrote copying as markdown",
            "closed three selection bugs",
            "@boris set up nightly builds",
            "added search in the feed"
        ],
        Summary: "Today's summary:",
        Quote: "The main thing is not to redraw the whole feed.",
        Minutes: "Meeting minutes",
        Topic: "Topic",
        Item: "Item");

    private static readonly ChatPhrases Russian = new(
        Replies:
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
        ],
        Sentences:
        [
            "Перенёс **разбор** в отдельный метод, интерфейс больше не замирает.",
            "Проверь `MdBlockPanel`: созданные блоки теперь держатся, пока не уйдут далеко.",
            "@vera посмотри, пожалуйста, ревью — там пара вопросов по API.",
            "Документация лежит в [вики](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki).",
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
        ],
        Points:
        [
            "поправили виртуализацию ленты",
            "ускорили разбор длинных абзацев",
            "@anna добавила тёмную тему",
            "переписали копирование в markdown",
            "закрыли три бага с выделением",
            "@boris настроил ночные сборки",
            "добавили поиск по ленте"
        ],
        Summary: "Итоги дня:",
        Quote: "Главное — не перерисовывать ленту целиком.",
        Minutes: "Протокол встречи",
        Topic: "Вопрос",
        Item: "Пункт");

    /// <summary>Логины участников; имена — строки витрины <c>People.&lt;логин&gt;</c>.</summary>
    public static IReadOnlyList<string> Logins { get; } = ["me", "anna", "boris", "vera"];

    /// <summary>Имя участника на текущем языке.</summary>
    public static string NameOf(string login) => Localizer.Text($"People.{login}");

    /// <summary>История из <paramref name="count"/> сообщений по возрастанию времени, на текущем языке.</summary>
    public static IReadOnlyList<ChatMessage> Generate(int count)
    {
        var phrases = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru" ? Russian : English;
        var stampFormat = Localizer.Text("Chat.Stamp");
        var random = new Random(7);
        var time = new DateTime(2026, 3, 2, 9, 0, 0);
        var messages = new List<ChatMessage>(count);

        for (var i = 0; i < count; i++)
        {
            time = time.AddMinutes(random.Next(1, 40));
            var stamp = time.ToString(stampFormat, CultureInfo.CurrentUICulture);
            if (i == count - MinutesFromEnd)
            {
                messages.Add(new(NameOf("me"), stamp, Minutes(phrases, random, 40), true));
                continue;
            }

            var login = Logins[random.Next(Logins.Count)];
            var text = Text(phrases, random);
            while (text.Contains('@' + login, StringComparison.Ordinal))
            {
                text = Text(phrases, random);
            }

            messages.Add(new(NameOf(login), stamp, text, login == "me"));
        }

        return messages;
    }

    private static string Text(ChatPhrases phrases, Random random) => random.Next(100) switch
    {
        < 45 => phrases.Replies[random.Next(phrases.Replies.Length)],
        < 85 => Paragraph(phrases, random, 1 + random.Next(3)),
        < 97 => Summary(phrases, random),
        _ => Minutes(phrases, random, 20 + random.Next(30))
    };

    /// <summary>Абзац из нескольких фраз.</summary>
    private static string Paragraph(ChatPhrases phrases, Random random, int sentences)
        => string.Join(" ", Enumerable.Range(0, sentences).Select(_ => phrases.Sentences[random.Next(phrases.Sentences.Length)]));

    /// <summary>Итоги: список, иногда код и цитата, абзац в конце.</summary>
    private static string Summary(ChatPhrases phrases, Random random)
    {
        var text = new StringBuilder(phrases.Summary).Append("\n\n");
        foreach (var point in phrases.Points.OrderBy(_ => random.Next()).Take(2 + random.Next(4)))
        {
            text.Append("- ").Append(point).Append('\n');
        }

        if (random.Next(2) == 0)
        {
            text.Append("\n```csharp\nvar viewer = new MarkdownViewer { Markdown = message.Text };\n```\n");
        }

        if (random.Next(3) == 0)
        {
            text.Append("\n> ").Append(phrases.Quote).Append('\n');
        }

        return text.Append('\n').Append(Paragraph(phrases, random, 2)).ToString();
    }

    /// <summary>Протокол встречи: <paramref name="items"/> пунктов по абзацу, по шесть под заголовком.</summary>
    private static string Minutes(ChatPhrases phrases, Random random, int items)
    {
        var text = new StringBuilder("**").Append(phrases.Minutes).Append("**");
        for (var i = 1; i <= items; i++)
        {
            if (i % 6 == 1)
            {
                text.Append("\n\n### ").Append(phrases.Topic).Append(' ').Append(i / 6 + 1);
            }

            text.Append("\n\n**").Append(phrases.Item).Append(' ').Append(i).Append(".** ").Append(Paragraph(phrases, random, 2 + random.Next(3)));
        }

        return text.ToString();
    }
}
