[English](Links-and-Anchors) | **Русский**

Ссылки во вьюере работают как в браузере: ссылки наружу открываются снаружи, якоря прокручивают
документ. Заголовки дают готовое оглавление.

## Что делает щелчок

1. Вьюер поднимает маршрутизируемое событие `MarkdownLink.Navigate`. Если приложение его обработало,
   больше ничего не происходит — см. [Ссылки приложения](Application-Links-ru).
2. Ссылка на якорь этого документа (`#...`) прокручивает к якорю.
3. Остальное получает открыватель ссылок. Стандартный открывает системным обработчиком адреса веба и
   почты — `http`, `https`, `mailto` и `www.…` без схемы. Другие схемы и пути к файлам не
   открываются: это дело приложения.

Заголовок ссылки, `[text](https://example.com "title")`, — её подсказка.

## Якоря

Каждый заголовок — якорь. Идентификатор у него как у GitHub: текст в нижнем регистре, где буквы,
цифры, `-` и `_` остаются, пробелы становятся `-`, а прочие знаки выпадают; повтор заголовка получает
`-1`, `-2`. Явный
идентификатор задают атрибутами: `## Setup {#setup}`. Сноски — тоже якоря: `fn:1` — первая сноска,
`fnref:1` — первая ссылка на сноску.

**Любой `.md`-файл**:

```markdown
See [installation](#installation) or [the details](#advanced).

## Installation

...

## Advanced setup {#advanced}
```

## Прокрутка к месту

| Метод | Куда прокручивает |
|---|---|
| `ScrollToAnchor(id, fraction)` | к заголовку или сноске; `false` — такого якоря нет |
| `ScrollToRange(range, fraction)` | к куску [текстовой карты](Selection-ru#позиции-текстовая-карта) |
| `ScrollToSourceLine(line, fraction)` | к строке исходника — см. [Строки исходника и две панели](Source-Lines-and-Two-Panes-ru) |

`fraction` — где цель встанет в видимой области: `0` — наверху, `0.5` — посередине. Цель может лежать
далеко за краем, среди ещё не созданных блоков. Вьюер двигает прокрутку вокруг себя в пикселях,
поэтому в списке нужен `VirtualizingPanel.ScrollUnit="Pixel"` — см.
[Прокрутку и виртуализацию](Scrolling-and-Virtualization-ru).

**`MainWindow.xaml.cs`**, например в обработчике кнопки:

```csharp
Viewer.ScrollToAnchor("installation", 0.1);
```

## Оглавление

`TextMap.Headings` перечисляет заголовки документа: `Level`, `Text`, якорь `Id`, кусок карты `Range`
и `SourceLine`. Список, привязанный к нему, — это оглавление; `ViewportChanged` и
`GetVisibleSourceLine()` говорят, какой заголовок читают сейчас.

**`MainWindow.xaml`**:

```xml
<DockPanel>
    <ListBox x:Name="Contents"
             DockPanel.Dock="Left"
             Width="220"
             ItemsSource="{Binding TextMap.Headings, ElementName=Viewer}"
             DisplayMemberPath="Text"
             SelectionChanged="OnContentsSelectionChanged" />
    <ScrollViewer>
        <mdv:MarkdownViewer x:Name="Viewer"
                            Markdown="{Binding Text}"
                            ViewportChanged="OnViewportChanged"
                            Margin="24,16" />
    </ScrollViewer>
</DockPanel>
```

**`MainWindow.xaml.cs`**, в классе окна (`using MarkdigWpfViewer.Selection;`):

```csharp
private bool _followingScroll;

private void OnContentsSelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (!_followingScroll && Contents.SelectedItem is MdHeading heading)
    {
        Viewer.ScrollToRange(heading.Range);
    }
}

private void OnViewportChanged(object? sender, EventArgs e)
{
    if (Viewer.GetVisibleSourceLine() is not { } line)
    {
        return;
    }

    _followingScroll = true;
    Contents.SelectedItem = Viewer.TextMap.Headings.LastOrDefault(heading => heading.SourceLine <= line + 0.5);
    _followingScroll = false;
}
```

Список обновляется сам, когда меняется текст: новый текст строит новую карту. Чтобы вложенные
заголовки шли с отступом, отступ пункта привязывают к `Level` через конвертер.
