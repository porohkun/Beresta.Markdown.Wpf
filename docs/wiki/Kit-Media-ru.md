[English](Kit-Media) | **Русский**

Готовые расширения для медиа и данных: карточки видео, эмодзи-шорткоды, шорткоды Hugo и Jekyll,
таблицы из CSV и карточка свойств для front matter. По умолчанию выключены и подключаются каждое одной
строкой (`xmlns:kit="clr-namespace:MarkdigWpfViewer.Kit;assembly=Markdig.Wpf.Viewer"`), как
[плашки](Kit-Callouts-ru#подключение).

**`MainWindow.xaml`**:

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}">
    <mdv:MarkdownViewer.Extensions>
        <kit:VideoCards />
        <kit:EmojiShortcodes />
        <kit:Shortcodes />
        <kit:CsvTables />
        <kit:PropertiesCard />
    </mdv:MarkdownViewer.Extensions>
</mdv:MarkdownViewer>
```

## Карточки видео

`VideoCards`: картинка-ссылка на видео YouTube, Vimeo или RuTube — так видео показывают в README —
становится карточкой: превью, название из alt и значок воспроизведения. Картинка, у которой адрес —
видео, тоже становится карточкой: превью по адресу видео даёт YouTube, Vimeo и RuTube не дают, и их
карточка без превью. Щелчок идёт путём [ссылок](Application-Links-ru): событие `Navigate`, затем
открыватель ссылок.

**Любой `.md`-файл**:

```markdown
[![Обзор релиза](cover.png)](https://www.youtube.com/watch?v=VIDEO_ID)

![Демо](https://youtu.be/VIDEO_ID)
```

Превью грузит [загрузчик картинок](Images-ru#свой-загрузчик) вьюера. `LoadThumbnails="False"` не пускает
карточки в сеть: превью из интернета не грузится, локальное — грузится.

```xml
<kit:VideoCards LoadThumbnails="False" />
```

## Эмодзи-шорткоды

`EmojiShortcodes`: имена эмодзи GitHub — `:smile:`, `:rocket:`, `:white_check_mark:` — становятся
эмодзи. Смайлики вида `:)` остаются текстом.

WPF рисует эмодзи одним цветом: цветных шрифтов в нём нет, и эмодзи берёт цвет текста вокруг.

## Шорткоды

`Shortcodes`: `{{< имя аргументы >}}` и `{{% имя аргументы %}}` из Hugo и `{% имя аргументы %}` из
Jekyll — виджеты по имени. Аргументы делятся пробелами, аргумент с пробелами берётся в кавычки,
`ключ=значение` — именованный аргумент. Шорткод, который никто не обрабатывает, остаётся текстом.

| Имя | Аргументы | Становится |
|---|---|---|
| `youtube` | id видео, название | карточкой видео |
| `vimeo` | id видео, название | карточкой видео без превью |
| `gist` | `user/id` или `user id` | карточкой-ссылкой на gist |
| `pdf` | адрес | карточкой-ссылкой с именем файла |
| `toc` | — | [оглавлением](Kit-Links-ru#оглавление) на месте, когда шорткод — отдельный абзац |

**Любой `.md`-файл**:

```markdown
{{< youtube VIDEO_ID "Обзор релиза" >}}

{% pdf "docs/guide.pdf" %}

{{< toc >}}
```

Карточки видео из шорткодов слушаются `LoadThumbnails` у `Shortcodes` — так же, как в `VideoCards`.

Свои имена приложение добавляет в `Handlers`. Обработчик получает шорткод — имя, аргументы,
именованные аргументы — и возвращает модель виджета: строчный виджет встаёт в строку, блочный —
вместо абзаца, в котором только этот шорткод. `null` оставляет шорткод текстом.

**`MainWindow.xaml.cs`**, в конструкторе окна после `InitializeComponent()`
(`using MarkdigWpfViewer.Kit;`, `using MarkdigWpfViewer.ViewModels;`):

```csharp
var shortcodes = new Shortcodes();
shortcodes.Handlers["kbd"] = shortcode => new RunVm(string.Join(" ", shortcode.Arguments), Kbd: true);
Viewer.Extensions.Add(shortcodes);
```

Теперь `{{< kbd Ctrl+Shift+P >}}` — клавиша. `Argument(имя, номер)` возвращает именованный аргумент, а
без него — аргумент на этом месте. Виджет со своей моделью и шаблоном делается, как в
[Расширениях](Extensions-ru).

## Таблицы из CSV

`CsvTables`: блок кода на `csv` или `tsv` — таблица, первая строка — заголовок. Значения — по правилам
CSV: значение в кавычках держит запятые и переносы строк, `""` внутри него — кавычка; пробелы вокруг
значения без кавычек отбрасываются. Короткие строки добираются пустыми ячейками до самой широкой.

**Любой `.md`-файл**:

````markdown
```csv
Город,Население,"Площадь, км²"
Москва,13 010 112,2561
Новосибирск,1 633 595,502
```
````

Это обычная таблица темы: выделяется по ячейкам и копируется как Markdown таблицей.

## Карточка свойств

`PropertiesCard`: [front matter](Supported-Markdown-ru#front-matter) в начале документа — таблица
«ключ — значение». Значения разбираются только для показа: `ключ: значение` в кавычках и без; списки
`[a, b]` и строки `- a` — одной строкой через запятую; текст `|` и `>` сохраняет строки; вложенное
показывается текстом. Разобранные данные наружу не отдаются: текст блока по-прежнему в `FrontMatter`,
а разбор YAML — дело приложения.

**Любой `.md`-файл**:

```markdown
---
title: Заметки к релизу
tags: [релиз, wpf]
author:
  name: Иван
---
```

Скопированная как Markdown целиком, карточка отдаёт front matter, как он написан.

## Вид

| Ключ | Что это |
|---|---|
| `Md.VideoCard.Template` | шаблон карточки видео; `Md.VideoCard.Width`, `.PreviewHeight`, `.CornerRadius`, `.TitlePadding`, `.PreviewBackground`, `.PlayBackground`, `.PlayForeground`, `.PlayIcon` |
| `Md.VideoCard.PreviewStyle` | стиль `MdImage` для превью |
| `Md.LinkCard.Template` | шаблон карточки-ссылки; `Md.LinkCard.Padding`, `.CornerRadius`, `.AddressForeground`, `.AddressFontSize`, `.Icon` |
| `Md.Properties.Template` | шаблон карточки свойств; `Md.Properties.KeyStyle` — стиль `MdTextBlock` для ключа |

Рамки карточек — `Md.Table.BorderBrush`, столбец ключей карточки свойств — `Md.Table.HeaderBackground`.
