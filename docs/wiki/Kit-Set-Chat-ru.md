[English](Kit-Set-Chat) | **Русский**

`ChatSet` рисует разметку, общую для чатов — Slack, Discord, Telegram: упоминания, спойлеры и эмодзи.
Чат — это список коротких документов: каждое сообщение — в свой вьюер внутри виртуализированного
списка, см. [Прокрутку и виртуализацию](Scrolling-and-Virtualization-ru).

![Сообщения в синтаксисе чата](images/kit-chat.png)

## Подключение

**`MainWindow.xaml`** (`xmlns:kit="clr-namespace:MarkdigWpfViewer.Kit;assembly=Markdig.Wpf.Viewer"` на
корневом элементе):

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}">
    <mdv:MarkdownViewer.Extensions>
        <kit:ChatSet />
    </mdv:MarkdownViewer.Extensions>
</mdv:MarkdownViewer>
```

`Resolver` даёт имена людей; щелчок по упоминанию приходит приложению как `mention:имя` — см.
[Связи](Kit-Links-ru).

## Синтаксис

| Markdown | На экране | Часть |
|---|---|---|
| `@имя` | чип с именем человека | [упоминания](Kit-Links-ru#упоминания) |
| `\|\|спойлер\|\|` | текст закрашен, пока по нему не щёлкнут | [спойлеры](Kit-Text-ru#спойлеры) |
| `:smile:` | эмодзи | [эмодзи](Kit-Media-ru#эмодзи-шорткоды) |

**Любой `.md`-файл**:

```markdown
@anna the build is green :white_check_mark:

The answer is ||42|| :wink:
```
