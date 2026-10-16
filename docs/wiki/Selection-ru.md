[English](Selection) | **Русский**

Текст во вьюере выделяется как в одном документе, а не как в отдельных полях: одно выделение идёт
сквозь абзацы, списки, таблицы и код, в том числе сквозь блоки, прокрученные за край.

![Выделение сквозь блоки и подсвеченное найденное](images/selection.png)

## Мышь и клавиатура

| Действие | Что выходит |
|---|---|
| протянуть мышью | выделение от точки нажатия до указателя; за краем прокрутки страница прокручивается следом |
| Shift + щелчок | конец выделения переезжает к щелчку |
| двойной щелчок | выделяет слово |
| тройной щелчок | выделяет абзац, заголовок, ячейку таблицы или блок кода |
| Ctrl+A | выделяет весь документ |
| Ctrl+C, Ctrl+Shift+C | копируют — см. [Копирование](Copying-ru) |

На интерактивных элементах выделение не начинается: кнопки, флажки, поля ввода, списки, полосы
прокрутки. Свой элемент, который сам обрабатывает мышь, получает то же обращение через
`sel:MdSelection.IsInteractive`.

**Шаблон своего виджета** (см. [Расширения](Extensions-ru)):

```xml
<Canvas sel:MdSelection.IsInteractive="True" MouseLeftButtonDown="OnSketchMouseDown" />
```

## Позиции: текстовая карта

Выделение — это кусок позиций **текстовой карты**: текста всего документа одной строкой,
`Viewer.TextMap.Text`. Это тот текст, который видно на экране: без разметки Markdown, блоки разделены
переводами строк, ячейки таблицы — табуляцией, картинка или неделимый виджет — один знак
`MdTextMap.ObjectMark`. Карта покрывает и несозданные блоки, поэтому искать в ней можно обычными
строковыми методами. Каждое новое значение `Markdown` строит новую карту.

## Выделение из кода

`Select(start, length)`, `SelectAll()` и `ClearSelection()` меняют выделение, `ScrollToRange`
прокручивает к нему.

**`MainWindow.xaml.cs`**, в классе окна (`using MarkdigWpfViewer.Selection;`):

```csharp
private void SelectFirst(string word)
{
    var index = Viewer.TextMap.Text.IndexOf(word, StringComparison.OrdinalIgnoreCase);
    if (index >= 0)
    {
        Viewer.Select(index, word.Length);
        Viewer.ScrollToRange(new MdTextRange(index, word.Length), 0.3);
    }
}
```

## Что выделено

| Член | Что даёт |
|---|---|
| `SelectionStart`, `SelectionLength` | кусок текстовой карты; свойства зависимостей, к ним можно привязаться |
| `SelectedText` | выделенный текст, как его видит пользователь |
| `SelectedMarkdown` | разметка выделенного — см. [Копирование](Copying-ru) |
| `SelectionChanged` | событие после того, как выделение сдвинулось |

**`MainWindow.xaml`**, строка состояния рядом с вьюером:

```xml
<TextBlock Text="{Binding SelectionLength, ElementName=Viewer, StringFormat='Selected: {0}'}" />
```

Новый текст сохраняет выделение, пока живы его блоки; исчезли блоки — выделение снимается.

## Выключить выделение

`IsSelectionEnabled="False"` снимает выделение, и ни мышь, ни Ctrl+A, ни `Select` ничего не
выделяют, пока его не вернут.

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}" IsSelectionEnabled="False" />
```

Цвет выделения — ключ темы `Md.Selection.Brush` или свойство вьюера `SelectionBrush`. Кисть рисуется
под текстом, поэтому её делают полупрозрачной.

## Где текст на экране

`GetRangeRects(range)` возвращает прямоугольники куска по строкам, `GetRangeBounds(range)` — их общую
рамку; оба в координатах вьюера. Есть они только у созданных блоков: кусок за пределами видимого даёт
пустой список и `Rect.Empty`. Этого хватает, чтобы поставить кнопку рядом с выделением.

**`MainWindow.xaml`**, вьюер и холст над ним в одной ячейке сетки:

```xml
<ScrollViewer>
    <Grid Margin="24,16">
        <mdv:MarkdownViewer x:Name="Viewer"
                            Markdown="{Binding Text}"
                            SelectionChanged="OnSelectionMoved"
                            ViewportChanged="OnSelectionMoved" />
        <Canvas>
            <Button x:Name="AskButton" Content="Explain" Visibility="Collapsed" Focusable="False" />
        </Canvas>
    </Grid>
</ScrollViewer>
```

**`MainWindow.xaml.cs`**, в классе окна (`using MarkdigWpfViewer.Selection;`):

```csharp
private void OnSelectionMoved(object? sender, EventArgs e)
{
    var bounds = Viewer.GetRangeBounds(new MdTextRange(Viewer.SelectionStart, Viewer.SelectionLength));
    AskButton.Visibility = bounds.IsEmpty ? Visibility.Collapsed : Visibility.Visible;
    if (!bounds.IsEmpty)
    {
        Canvas.SetLeft(AskButton, bounds.Left);
        Canvas.SetTop(AskButton, bounds.Bottom + 4);
    }
}
```

Обратное направление — `GetPositionFromPoint(point, snapToText)`: позиция текстовой карты под точкой.
С `snapToText: false` точка вне текста даёт `null`, с `true` — ближайший текст. Вместе со строками
исходника из карты это говорит, какая строка исходника под мышью.

**`MainWindow.xaml.cs`**, обработчик `MouseMove` вьюера; `StatusText` — `TextBlock` окна:

```csharp
private void OnViewerMouseMove(object sender, MouseEventArgs e)
{
    StatusText.Text = Viewer.GetPositionFromPoint(e.GetPosition(Viewer), snapToText: false) is { } position
                      && Viewer.TextMap.SourceLineOf(position) is { } line
        ? $"Source line {line + 1}"
        : string.Empty;
}
```
