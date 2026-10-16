[English](Kit-Set-Obsidian) | **Русский**

`ObsidianSet` рисует заметки Obsidian: свойства, плашки, комментарии, встраивание картинок,
вики-ссылки, теги и статусы задач. `==выделению==` и сноскам набор не нужен. Формул и диаграмм в
наборе нет: это работа для [своих расширений](Extensions-ru).

![Заметка в синтаксисе Obsidian](images/kit-obsidian.png)

## Подключение

**`MainWindow.xaml`** (`xmlns:kit="clr-namespace:MarkdigWpfViewer.Kit;assembly=Markdig.Wpf.Viewer"` на
корневом элементе):

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}">
    <mdv:MarkdownViewer.Extensions>
        <kit:ObsidianSet />
    </mdv:MarkdownViewer.Extensions>
</mdv:MarkdownViewer>
```

`Resolver` знает, какие страницы есть: ссылка на несуществующую получает свой цвет. Щелчок по
вики-ссылке или тегу приходит приложению как `wiki:Страница#Раздел` или `tag:имя` — см.
[Связи](Kit-Links-ru).

## Синтаксис

| Markdown | На экране | Часть |
|---|---|---|
| `---` … `---` в начале | таблица «ключ — значение» | [карточка свойств](Kit-Media-ru#карточка-свойств) |
| `> [!tip] Заголовок`, `> [!faq]-`, `> [!todo]+` | плашка своего вида; `-` и `+` делают её сворачиваемой | [плашки Obsidian](Kit-Callouts-ru#плашки-obsidian) |
| `%%комментарий%%`, блок между строками `%%` | ничего | [комментарии](Kit-Text-ru#комментарии) |
| `![[картинка.png]]`, `![[картинка.png\|200]]` | картинка; встраивание не картинки — вики-ссылка | [встраивание картинок](Kit-Links-ru#встраивание-картинок) |
| `[[Страница]]`, `[[Страница#Раздел]]`, `[[Страница\|подпись]]`, `[[#Заголовок]]` | ссылка | [вики-ссылки](Kit-Links-ru#вики-ссылки) |
| `#тег`, `#вложенный/тег` | тег | [теги](Kit-Links-ru#теги) |
| `- [/]`, `- [-]`, `- [>]`, `- [?]`, `- [!]` | значок статуса | [статусы задач](Kit-Text-ru#статусы-задач) |

**Любой `.md`-файл**:

```markdown
---
tags: [project, wpf]
status: draft
---

> [!tip] Daily note
> Links to [[Roadmap]], [[Missing page]] and the section [[#Tasks]].

Tagged #idea %%a hidden comment%% with an embedded image:

![[banner.png|240]]

## Tasks

- [/] in progress
- [>] moved
- [!] important
```
