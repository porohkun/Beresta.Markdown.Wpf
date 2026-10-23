# Beresta.Markdown.Wpf v0.9.1

[![NuGet](https://img.shields.io/nuget/v/Beresta.Markdown.Wpf)](https://www.nuget.org/packages/Beresta.Markdown.Wpf)
[![Загрузки](https://img.shields.io/nuget/dt/Beresta.Markdown.Wpf)](https://www.nuget.org/packages/Beresta.Markdown.Wpf)
[![Публикация](https://img.shields.io/github/actions/workflow/status/porohkun/Beresta.Markdown.Wpf/nuget.yml?branch=main&label=publish)](https://github.com/porohkun/Beresta.Markdown.Wpf/actions/workflows/nuget.yml)
![.NET 7, 8, 9, 10](https://img.shields.io/badge/.NET-7%20%7C%208%20%7C%209%20%7C%2010-512BD4)
[![Лицензия](https://img.shields.io/github/license/porohkun/Beresta.Markdown.Wpf)](https://github.com/porohkun/Beresta.Markdown.Wpf/blob/main/LICENSE)

[English](https://github.com/porohkun/Beresta.Markdown.Wpf/blob/main/README.md) | **Русский**

Быстрый нативный вьюер Markdown для WPF — без браузера и без FlowDocument внутри: документ рисуется
настоящими контролами и шаблонами WPF. Огромные документы виртуализируются, предпросмотр и потоковые
ответы чата обновляются на лету, текст выделяется сквозь блоки, а оформление и синтаксис полностью
настраиваются и расширяются. Текст разбирает [Markdig](https://github.com/xoofx/markdig). Второй
пакет, `Beresta.Markdown.Wpf.Editor`, правит исходник прямо там, где он нарисован.

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
- **Редактор в том же рисунке.** Блоки под кареткой показывают свой Markdown в AvalonEdit, остальной
  документ остаётся нарисованным; исходник сохраняет вид отрисовки — от таблиц столбцами до подсвеченного
  кода, — а редактор ищет и заменяет, вставляет HTML Markdown-ом и правит front matter по типам —
  [Редактор](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Editor-ru).

## Установка

```bash
dotnet add package Beresta.Markdown.Wpf
```

Нужны .NET 7 или новее (`net7.0-windows`) и WPF.

Документация IntelliSense — на английском и русском: Visual Studio с русским интерфейсом показывает
русскую.

## Подключение темы

Тему вьюера один раз вливают в ресурсы приложения — без неё вьюер вместо документа показывает
подсказку с этим адресом.

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
        xmlns:md="https://github.com/porohkun/Beresta.Markdown.Wpf"
        Title="Markdown" Width="800" Height="600">
    <ScrollViewer>
        <md:MarkdownViewer Markdown="{Binding Text}" Margin="24,16" />
    </ScrollViewer>
</Window>
```

Шрифт и цвет текста вьюер берёт у приложения.

## Синтаксис других программ

Готовые расширения рисуют синтаксис GitHub, GitLab, Obsidian, MkDocs, Docusaurus, HackMD и чатов. По
умолчанию они выключены; набор подключает синтаксис одной программы.

`MainWindow.xaml` (`xmlns:md="https://github.com/porohkun/Beresta.Markdown.Wpf"` на
корневом элементе):

```xml
<md:MarkdownViewer Markdown="{Binding Text}">
    <md:MarkdownViewer.Extensions>
        <md:ObsidianSet />
    </md:MarkdownViewer.Extensions>
</md:MarkdownViewer>
```

## Правка Markdown

Второй пакет правит исходник прямо там, где он нарисован. Он приносит вьюер той же версии, AvalonEdit и
ReverseMarkdown для вставки HTML Markdown-ом.

```bash
dotnet add package Beresta.Markdown.Wpf.Editor
```

`App.xaml`, после темы вьюера:

```xml
<ResourceDictionary Source="pack://application:,,,/Beresta.Markdown.Wpf.Editor;component/Themes/Generic.xaml" />
```

`MainWindow.xaml`:

```xml
<ScrollViewer>
    <md:MarkdownEditor Markdown="{Binding Text, Mode=TwoWay}" EditMode="Blocks" Margin="24,16" />
</ScrollViewer>
```

Режимы, клавиши, отмена, front matter и вставка с форматированием — [Редактор](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Editor-ru);
поиск и замена — [Пометки и поиск](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Search-Highlighting-ru#поиск-и-замена-в-редакторе).

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
- **Правка:** [Редактор](https://github.com/porohkun/Beresta.Markdown.Wpf/wiki/Editor-ru).
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
