# Beresta.Markdown.Wpf v0.7.0

[English](https://github.com/porohkun/Beresta.Markdown.Wpf/blob/main/README.md) | **Русский**

Контрол WPF, который показывает Markdown. Текст разбирает [Markdig](https://github.com/xoofx/markdig), а
рисует вьюер — контролами и шаблонами WPF.

![Вьюер в теме витрины](https://raw.githubusercontent.com/wiki/porohkun/Beresta.Markdown.Wpf/images/overview-ru.png)

## Чем он хорош

- **Свой быстрый рендер родными элементами WPF.** Markdown становится настоящими контролами WPF — без
  браузера и HTML посередине, [Как рисует вьюер](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/How-It-Renders-ru).
- **Полная виртуализация.** Создаются только видимые блоки: в `ScrollViewer`, на странице рядом с
  другим содержимым, в списке сообщений — [Прокрутка и виртуализация](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Scrolling-and-Virtualization-ru).
- **Выделение как в документе.** Одно выделение идёт сквозь абзацы, списки, таблицы и код, даже через
  блоки за краем экрана; копирование кладёт в буфер обычный текст, Markdown и HTML —
  [Выделение](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Selection-ru), [Копирование](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Copying-ru).
- **Легко расширять.** Свой синтаксис становится своими виджетами, а встроенный можно перехватить —
  [Расширения](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Extensions-ru).
- **Легко перекрашивать.** Каждый цвет, шрифт и отступ — ключ ресурса, любому виду блока можно дать
  свой шаблон — [Тема](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Theming-ru), [Стили и шаблоны](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Styles-and-Templates-ru).
- **Много элементов разметки — подключаешь только нужные.** CommonMark, расширения Markdig и HTML из
  README — из коробки; синтаксис GitHub, GitLab, Obsidian, MkDocs, Docusaurus, HackMD и чатов —
  готовыми расширениями, по одному или наборами, [Что поддерживается](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Supported-Markdown-ru).

## Установка

```bash
dotnet add package Beresta.Markdown.Wpf
```

Нужны .NET 7 или новее (`net7.0-windows`) и WPF.

Документация IntelliSense — на английском и русском: Visual Studio с русским интерфейсом показывает
русскую.

## Подключение темы

Тему вьюера один раз вливают в ресурсы приложения — без неё вьюер пуст.

`App.xaml`:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="pack://application:,,,/Beresta.Markdown.Wpf;component/Themes/Generic.xaml" />
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
        xmlns:mdv="clr-namespace:Beresta.Markdown.Wpf;assembly=Beresta.Markdown.Wpf"
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

`MainWindow.xaml` (`xmlns:kit="clr-namespace:Beresta.Markdown.Wpf.Kit;assembly=Beresta.Markdown.Wpf"` на
корневом элементе):

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}">
    <mdv:MarkdownViewer.Extensions>
        <kit:ObsidianSet />
    </mdv:MarkdownViewer.Extensions>
</mdv:MarkdownViewer>
```

## Документация

В [вики](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Home-ru) каждая возможность описана с рабочим примером; все настройки и места перехвата
собраны на одной странице — [Настройки и перехват](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Settings-and-Hooks-ru).

- **Основы:** [Начало работы](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Getting-Started-ru),
  [Как рисует вьюер](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/How-It-Renders-ru),
  [Что поддерживается](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Supported-Markdown-ru),
  [Настройки и перехват](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Settings-and-Hooks-ru),
  [Прокрутка и виртуализация](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Scrolling-and-Virtualization-ru),
  [Сравнение](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Comparison-ru).
- **Работа с текстом:** [Выделение](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Selection-ru),
  [Копирование](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Copying-ru),
  [Ссылки и якоря](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Links-and-Anchors-ru),
  [Пометки и поиск](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Search-Highlighting-ru),
  [Доступность](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Accessibility-ru).
- **Оформление:** [Тема](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Theming-ru),
  [Стили и шаблоны](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Styles-and-Templates-ru),
  [Блоки кода](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Code-Blocks-ru),
  [Номера строк](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Line-Numbers-ru).
- **Встраивание в приложение:** [Ссылки приложения](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Application-Links-ru),
  [Картинки](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Images-ru),
  [Чужой текст](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Untrusted-Text-ru),
  [Списки задач](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Task-Lists-ru),
  [Строки исходника и две панели](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Source-Lines-and-Two-Panes-ru),
  [Координаты](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Coordinates-ru),
  [Живой предпросмотр](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Live-Preview-ru).
- **Свой синтаксис:** [Расширения](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Extensions-ru).
- **Готовые расширения:** [Наборы](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Kit-Sets-ru),
  [Плашки и сворачиваемое](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Kit-Callouts-ru),
  [Связи](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Kit-Links-ru),
  [Чипы и работа над текстом](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Kit-Text-ru),
  [Медиа и данные](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Kit-Media-ru).

В репозитории есть и витрина с этими сценариями:

```bash
dotnet run --project Beresta.Markdown.Wpf.Showcase
```

## Лицензия

[MIT](https://github.com/porohkun/Beresta.Markdown.Wpf/blob/main/LICENSE).
