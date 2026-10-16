[English](Kit-Set-GitLab) | **Русский**

`GitLabSet` рисует то, что GitLab добавляет к Markdown: оглавление на месте, ссылки на задачи, коды
цветов и эмодзи. Merge request `!123` остаётся текстом: ссылки на задачи знают только префиксы `#`,
`group/project#` и `PROJ-`.

![Документ в синтаксисе GitLab](images/kit-gitlab.png)

## Подключение

**`MainWindow.xaml`** (`xmlns:kit="clr-namespace:MarkdigWpfViewer.Kit;assembly=Markdig.Wpf.Viewer"` и
`xmlns:sys="clr-namespace:System;assembly=System.Runtime"` на корневом элементе):

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}">
    <mdv:MarkdownViewer.Extensions>
        <kit:GitLabSet>
            <kit:GitLabSet.IssueTemplates>
                <sys:String x:Key="#">https://gitlab.com/group/project/-/issues/{0}</sys:String>
            </kit:GitLabSet.IssueTemplates>
        </kit:GitLabSet>
    </mdv:MarkdownViewer.Extensions>
</mdv:MarkdownViewer>
```

Без шаблона `#123` остаётся текстом.

## Синтаксис

| Markdown | На экране | Часть |
|---|---|---|
| `[[_TOC_]]`, `[TOC]` отдельным абзацем | заголовки документа | [оглавление](Kit-Links-ru#оглавление) |
| `#123` | ссылка по твоему шаблону | [ссылки на задачи](Kit-Links-ru#ссылки-на-задачи) |
| `` `#FC6D26` ``, `` `hsl(15, 97%, 57%)` `` | код с кружком своего цвета | [коды цветов](Kit-Text-ru#коды-цветов) |
| `:rocket:` | эмодзи | [эмодзи](Kit-Media-ru#эмодзи-шорткоды) |

**Любой `.md`-файл**:

```markdown
[[_TOC_]]

## Staging

Fixes #42 :rocket: — the new badge color is `#FC6D26`.

## Production

Released after the review :white_check_mark:
```
