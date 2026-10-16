[English](Kit-Links) | **Русский**

Готовые расширения для связей между документами и людьми: вики-ссылки, встраивание картинок, теги,
упоминания, ссылки на задачи и оглавление на месте. По умолчанию выключены и подключаются каждое одной
строкой, как [плашки](Kit-Callouts-ru#подключение).

Смысл задаёт приложение. Щелчок по вики-ссылке, тегу или упоминанию идёт путём
[ссылок](Application-Links-ru): событие `MarkdownLink.Navigate` получает цель `wiki:Страница#Раздел`,
`tag:имя` или `mention:имя`, и обработчик приложения открывает то, что она значит. Есть ли страница и
как зовут человека, расширение спрашивает у резолвера — `IMdLinkResolver`, который реализует
приложение.

## Подключение

**`MainWindow.xaml`** (`xmlns:kit="clr-namespace:MarkdigWpfViewer.Kit;assembly=Markdig.Wpf.Viewer"` и
`xmlns:sys="clr-namespace:System;assembly=System.Runtime"` на корневом элементе; `local:NotesResolver` —
свой резолвер):

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}" mdv:MarkdownLink.Navigate="OnMarkdownLink">
    <mdv:MarkdownViewer.Extensions>
        <kit:WikiLinks Resolver="{x:Static local:NotesResolver.Instance}" />
        <kit:WikiEmbeds />
        <kit:Tags />
        <kit:Mentions Resolver="{x:Static local:NotesResolver.Instance}" />
        <kit:IssueLinks>
            <kit:IssueLinks.Templates>
                <sys:String x:Key="#">https://github.com/org/repo/issues/{0}</sys:String>
            </kit:IssueLinks.Templates>
        </kit:IssueLinks>
        <kit:TableOfContents />
    </mdv:MarkdownViewer.Extensions>
</mdv:MarkdownViewer>
```

**`NotesResolver.cs`**:

```csharp
using MarkdigWpfViewer.Kit;

public sealed class NotesResolver : IMdLinkResolver
{
    public static NotesResolver Instance { get; } = new();

    public bool PageExists(string page) => page is "Home" or "Roadmap";

    public string? DisplayName(string user) => user == "anna" ? "Anna Smith" : null;
}
```

У обоих членов есть поведение по умолчанию — все страницы есть, имя остаётся как написано, — поэтому
реализуют только нужное.

**`MainWindow.xaml.cs`**, в классе окна:

```csharp
private void OnMarkdownLink(object sender, MarkdownLinkEventArgs e)
{
    if (e.Href.StartsWith("wiki:") || e.Href.StartsWith("tag:") || e.Href.StartsWith("mention:"))
    {
        MessageBox.Show(e.Href);
        e.Handled = true;
    }
}
```

## Вики-ссылки

`WikiLinks`: `[[Страница]]`, `[[Страница#Раздел]]`, `[[Страница|подпись]]`. Ссылка — оформленный
текст: выделяется по буквам и находится поиском. Страница, которой резолвер не знает, рисуется своим
видом; `[[#Раздел]]` ведёт к заголовку этого документа.

**Любой `.md`-файл**:

```markdown
See [[Roadmap]], [[Roadmap#Stage 2|the next stage]] and [[#Links]].
```

## Встраивание картинок

`WikiEmbeds`: `![[файл.png]]` — картинка через [загрузчик картинок](Images-ru#откуда-берутся-картинки)
вьюера; `![[файл.png|200]]` и `![[файл.png|200x100]]` задают её размер в пикселях, любая другая подпись —
её alt. Встроенный файл, который не картинка, становится вики-ссылкой на него.

## Теги

`Tags`: `#тег` и `#тег/вложенный` — чипы с целью `tag:тег`. Не тег: одни цифры — это задача, —
`#` сразу после буквы или цифры, как в `C#`, и `#` внутри ссылки или кода.

## Упоминания

`Mentions`: `@имя` — чип с целью `mention:имя` и именем от резолвера. Не упоминание: `@` сразу после
буквы или цифры, как в адресе почты, и `@` внутри ссылки. Чип — шаблон темы `Md.Mention.Template`; с
прозрачным `Md.Mention.Background` он выглядит ссылкой.

## Ссылки на задачи

`IssueLinks` превращает `#123`, `org/repo#45` и `PROJ-45` в ссылки по шаблонам приложения. Шаблон —
префикс ровно как он написан перед номером — `#`, `org/repo#`, `PROJ-` — и адрес с `{0}` на месте
номера. По умолчанию шаблонов нет, и ничто не становится ссылкой, пока шаблон не добавлен.

**`MainWindow.xaml.cs`**, в конструкторе (`using MarkdigWpfViewer.Kit;`):

```csharp
var issues = new IssueLinks();
issues.Templates["#"] = "https://github.com/org/repo/issues/{0}";
issues.Templates["PROJ-"] = "https://jira.example.com/browse/PROJ-{0}";
Viewer.Extensions.Add(issues);
```

## Оглавление

`TableOfContents`: абзац ровно из `[TOC]`, `[[_TOC_]]` или `[[toc]]` становится списком заголовков
документа. Пункт ведёт к своему заголовку ссылкой `#id`, а список следует за текстом, когда тот
меняется.

## Вид

| Ключ | Что это |
|---|---|
| `Md.WikiLink.Style`, `Md.WikiLink.Missing.Style` | стили `Span` вики-ссылки и ссылки на страницу, которой нет |
| `Md.Tag.Style` | стиль `Span` тега |
| `Md.Mention.Template` | шаблон чипа упоминания; `Md.Mention.Background`, `.Foreground`, `.Padding`, `.CornerRadius` |
| `Md.Toc.Template` | шаблон оглавления; `Md.Toc.Margin`, `Md.Toc.Level1.Margin` … `Md.Toc.Level6.Margin` |

«Копировать как Markdown» пишет вики-ссылки, встраивания, теги и задачи обратно их синтаксисом,
упоминание — как `@имя`.
