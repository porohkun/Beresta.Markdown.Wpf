# Markdig.Wpf.Viewer v0.6.0

[English](https://github.com/porohkun/Markdig.Wpf.Viewer/blob/main/README.md) | **Русский**

Контрол WPF, который показывает Markdown родными элементами WPF: текст разбирает
[Markdig](https://github.com/xoofx/markdig), рисуют шаблоны WPF.

![Вьюер в теме витрины](https://raw.githubusercontent.com/porohkun/Markdig.Wpf.Viewer/main/docs/wiki/images/overview.png)

- **Виртуализация.** Вьюер живёт в прокрутке приложения и создаёт только видимые блоки: длинный
  документ, страница с другим содержимым или лента чата не тормозят.
- **Выделение.** Выделение идёт сквозь блоки; копирование кладёт в буфер обмена разом обычный текст,
  Markdown и HTML.
- **Навигация.** Ссылки, якоря, сноски, оглавление, прокрутка к строке исходника; любую ссылку может
  взять на себя приложение.
- **Тема.** Каждый цвет, шрифт и отступ — ключ ресурса; любому блоку можно дать свой шаблон.
- **Расширения.** Свой синтаксис Markdown превращается в свои виджеты.
- **Чужие диалекты.** Вики-ссылки, плашки, упоминания, шорткоды и таблицы из CSV в синтаксисе GitHub,
  GitLab, Obsidian, MkDocs, Docusaurus, HackMD и чатов идут в пакете: по умолчанию выключены, набор
  подключает синтаксис одной программы одной строкой.
- **Живой текст.** Новый текст ложится правками: годится для живого предпросмотра и ответов, которые
  приходят по кускам.

## Установка

```bash
dotnet add package Markdig.Wpf.Viewer
```

Или подключи репозиторий сабмодулем Git и сошлись на `Markdig.Wpf.Viewer/Markdig.Wpf.Viewer.csproj`.

Нужны .NET 10 (`net10.0-windows`) и WPF.

## Подключение темы

Тему вьюера один раз вливают в ресурсы приложения — без неё вьюер пуст.

`App.xaml`:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="pack://application:,,,/Markdig.Wpf.Viewer;component/Themes/Generic.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

## Показать документ

Своих полос прокрутки у вьюера нет: его ставят в `ScrollViewer`.

`MainWindow.xaml`:

```xml
<Window x:Class="YourApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:mdv="clr-namespace:MarkdigWpfViewer;assembly=Markdig.Wpf.Viewer"
        Title="Markdown" Width="800" Height="600">
    <ScrollViewer>
        <mdv:MarkdownViewer Markdown="{Binding Text}" Margin="24,16" />
    </ScrollViewer>
</Window>
```

Шрифт и цвет текста вьюер берёт у приложения.

## Синтаксис других программ

Готовые расширения рисуют синтаксис GitHub, GitLab, Obsidian, MkDocs, Docusaurus, HackMD и чатов. По
умолчанию они выключены; набор подключает синтаксис одной программы.

`MainWindow.xaml` (`xmlns:kit="clr-namespace:MarkdigWpfViewer.Kit;assembly=Markdig.Wpf.Viewer"` на
корневом элементе):

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}">
    <mdv:MarkdownViewer.Extensions>
        <kit:ObsidianSet />
    </mdv:MarkdownViewer.Extensions>
</mdv:MarkdownViewer>
```

## Документация

В [вики](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Home-ru) каждая возможность описана с
рабочим примером:

- **Основы:** [Начало работы](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Getting-Started-ru),
  [Что рисуется](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Supported-Markdown-ru),
  [Прокрутка и виртуализация](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Scrolling-and-Virtualization-ru).
- **Работа с текстом:** [Выделение](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Selection-ru),
  [Копирование](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Copying-ru),
  [Ссылки и якоря](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Links-and-Anchors-ru),
  [Доступность](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Accessibility-ru).
- **Оформление:** [Тема](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Theming-ru),
  [Стили и шаблоны](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Styles-and-Templates-ru),
  [Блоки кода](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Code-Blocks-ru),
  [Номера строк](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Line-Numbers-ru).
- **Расширение и интеграция:** [Расширения](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Extensions-ru),
  [Картинки](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Images-ru),
  [Списки задач](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Task-Lists-ru),
  [Ссылки приложения](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Application-Links-ru),
  [Строки исходника и две панели](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Source-Lines-and-Two-Panes-ru),
  [Живой предпросмотр](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Live-Preview-ru),
  [Подсветка найденного](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Search-Highlighting-ru).
- **Готовые расширения:** [Наборы](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Kit-Sets-ru),
  [Плашки и сворачиваемое](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Kit-Callouts-ru),
  [Связи](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Kit-Links-ru),
  [Чипы и работа над текстом](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Kit-Text-ru),
  [Медиа и данные](https://github.com/porohkun/Markdig.Wpf.Viewer/wiki/Kit-Media-ru).

В репозитории есть и витрина с этими сценариями:

```bash
dotnet run --project Markdig.Wpf.Viewer.Showcase
```

## Лицензия

[MIT](https://github.com/porohkun/Markdig.Wpf.Viewer/blob/main/LICENSE).
