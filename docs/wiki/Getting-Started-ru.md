[English](Getting-Started) | **Русский**

Вьюер — библиотека: её подключают к приложению WPF, один раз подключают тему и ставят
`MarkdownViewer` туда, где должен быть документ.

Нужны .NET 10 (`net10.0-windows`) и WPF.

## Установка

Пакетом NuGet.

**Терминал**, в папке проекта приложения:

```bash
dotnet add package Markdig.Wpf.Viewer
```

Или сабмодулем Git со ссылкой на проект — удобно, когда вьюер меняется вместе с приложением.

**Терминал**, в корне репозитория:

```bash
git submodule add https://github.com/porohkun/Markdig.Wpf.Viewer.git external/Markdig.Wpf.Viewer
dotnet add YourApp/YourApp.csproj reference external/Markdig.Wpf.Viewer/Markdig.Wpf.Viewer/Markdig.Wpf.Viewer.csproj
```

## Подключение темы

Вьюер рисует блоки шаблонами своей темы, `Themes/Generic.xaml`. Сам WPF её не подхватывает: тему
один раз вливают в ресурсы приложения. Без неё вьюер пуст.

**`App.xaml`**, ресурсы приложения:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="pack://application:,,,/Markdig.Wpf.Viewer;component/Themes/Generic.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

Свой словарь, который перекрывает ключи темы, идёт **после** этого — см. [Тема](Theming-ru).

## Показать документ

Своих полос прокрутки у вьюера нет: его ставят в `ScrollViewer`. Другие места для него — в
[Прокрутке и виртуализации](Scrolling-and-Virtualization-ru).

**`MainWindow.xaml`**, окно целиком:

```xml
<Window x:Class="YourApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:mdv="clr-namespace:MarkdigWpfViewer;assembly=Markdig.Wpf.Viewer"
        Title="Markdown" Width="800" Height="600">
    <ScrollViewer>
        <mdv:MarkdownViewer x:Name="Viewer" Margin="24,16" />
    </ScrollViewer>
</Window>
```

**`MainWindow.xaml.cs`**, в конструкторе после `InitializeComponent()`:

```csharp
Viewer.Markdown = """
    # Hello

    This is **Markdown** in a *WPF* window.

    - [x] theme connected
    - [ ] read the wiki
    """;
```

С MVVM текст привязывают как обычно: `Markdown="{Binding Text}"`. Новое значение ложится на
показанный документ правками, так что часто меняющийся текст — не проблема, см.
[Живой предпросмотр](Live-Preview-ru).

Шрифт и цвет текста вьюер берёт у приложения: наследует их от окна, как любой контрол, а сам задаёт
только размеры, отступы и акценты.

## Пространства имён XAML

Примеры в этой вики используют такие префиксы:

| Префикс | Пространство имён | Что в нём |
|---|---|---|
| `mdv` | `clr-namespace:MarkdigWpfViewer;assembly=Markdig.Wpf.Viewer` | `MarkdownViewer`, `MarkdownLink` |
| `s` | `clr-namespace:MarkdigWpfViewer.Styling;assembly=Markdig.Wpf.Viewer` | `MdStyleProvider` |
| `p` | `clr-namespace:MarkdigWpfViewer.Presenters;assembly=Markdig.Wpf.Viewer` | контролы для шаблонов блоков |
| `vm` | `clr-namespace:MarkdigWpfViewer.ViewModels;assembly=Markdig.Wpf.Viewer` | модели блоков и инлайнов |
| `mdx` | `clr-namespace:MarkdigWpfViewer.Extensions;assembly=Markdig.Wpf.Viewer` | `MdWidget`, `MdExtensionCollection` |
| `sel` | `clr-namespace:MarkdigWpfViewer.Selection;assembly=Markdig.Wpf.Viewer` | `MdSelection` |
| `b` | `clr-namespace:MarkdigWpfViewer.Behaviors;assembly=Markdig.Wpf.Viewer` | `NestedScroll` |
| `local` | `clr-namespace:YourApp` | свои классы из примеров |

Пространства имён C# называются так же: `MarkdigWpfViewer`, `MarkdigWpfViewer.Selection` и так далее.
