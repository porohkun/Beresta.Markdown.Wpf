[English](Kit-Sets) | **Русский**

Набор подключает готовые расширения одной программы одной строкой: синтаксис GitHub, GitLab,
Obsidian, MkDocs, Docusaurus, HackMD или чата. Порядок частей внутри набора задаёт библиотека. Набор
ставится в `Extensions`, как одиночное расширение.

**`MainWindow.xaml`** (`xmlns:kit="clr-namespace:MarkdigWpfViewer.Kit;assembly=Markdig.Wpf.Viewer"` на
корневом элементе):

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}">
    <mdv:MarkdownViewer.Extensions>
        <kit:ObsidianSet />
    </mdv:MarkdownViewer.Extensions>
</mdv:MarkdownViewer>
```

## Наборы

| Набор | Части по порядку | Настройки |
|---|---|---|
| [`GitHubSet`](Kit-Set-GitHub-ru) | [карточка свойств](Kit-Media-ru#карточка-свойств), [упоминания](Kit-Links-ru#упоминания), [ссылки на задачи](Kit-Links-ru#ссылки-на-задачи), [коды цветов](Kit-Text-ru#коды-цветов), [эмодзи](Kit-Media-ru#эмодзи-шорткоды) | `Resolver`, `IssueTemplates` |
| [`GitLabSet`](Kit-Set-GitLab-ru) | [оглавление](Kit-Links-ru#оглавление), ссылки на задачи, коды цветов, эмодзи | `IssueTemplates` |
| [`ObsidianSet`](Kit-Set-Obsidian-ru) | карточка свойств, [плашки Obsidian](Kit-Callouts-ru#плашки-obsidian), [комментарии](Kit-Text-ru#комментарии), [встраивание картинок](Kit-Links-ru#встраивание-картинок), [вики-ссылки](Kit-Links-ru#вики-ссылки), [теги](Kit-Links-ru#теги), [статусы задач](Kit-Text-ru#статусы-задач) | `Resolver` |
| [`MkDocsSet`](Kit-Set-MkDocs-ru) | [предупреждения MkDocs](Kit-Callouts-ru#предупреждения-mkdocs) | — |
| [`DocusaurusSet`](Kit-Set-Docusaurus-ru) | [контейнеры `:::вид`](Kit-Callouts-ru#контейнеры-вид); годится и для VuePress | — |
| [`HackMDSet`](Kit-Set-HackMD-ru) | контейнеры `:::вид`, оглавление, [шорткоды](Kit-Media-ru#шорткоды) | `ShortcodeHandlers`, `LoadThumbnails` |
| [`ChatSet`](Kit-Set-Chat-ru) | упоминания, [спойлеры](Kit-Text-ru#спойлеры), эмодзи | `Resolver` |

## Настройки

Настройки набора — это настройки его частей: `Resolver` — [резолвер](Kit-Links-ru#подключение)
упоминаний или вики-ссылок и встраиваний, `IssueTemplates` — шаблоны ссылок на задачи,
`ShortcodeHandlers` и `LoadThumbnails` — настройки шорткодов.

**`MainWindow.xaml`** (`xmlns:sys="clr-namespace:System;assembly=System.Runtime"` на корневом элементе;
`local:TeamResolver` — твой резолвер):

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}">
    <mdv:MarkdownViewer.Extensions>
        <kit:GitHubSet Resolver="{x:Static local:TeamResolver.Instance}">
            <kit:GitHubSet.IssueTemplates>
                <sys:String x:Key="#">https://github.com/org/repo/issues/{0}</sys:String>
            </kit:GitHubSet.IssueTemplates>
        </kit:GitHubSet>
    </mdv:MarkdownViewer.Extensions>
</mdv:MarkdownViewer>
```

## Выключение частей

`Without` называет части, которые набор не подключает, — по именам их расширений.

```xml
<kit:ObsidianSet Without="Comments, TaskStatuses" />
```

В коде это флаги `KitParts` (`using MarkdigWpfViewer.Kit;`):

```csharp
Viewer.Extensions.Add(new ObsidianSet { Without = KitParts.Comments | KitParts.TaskStatuses });
```

## Свои расширения в наборе

Набор — это [`MdExtensionSet`](Extensions-ru#наборы-расширений): расширение, добавленное в его
`Extensions`, опрашивается после частей набора.

**`MainWindow.xaml.cs`**, в конструкторе окна после `InitializeComponent()`
(`using MarkdigWpfViewer.Kit;`; `DiagramExtension` — твоё расширение):

```csharp
var set = new GitHubSet();
set.Extensions.Add(new DiagramExtension());
Viewer.Extensions.Add(set);
```
