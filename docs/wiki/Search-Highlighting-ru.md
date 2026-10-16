[English](Search-Highlighting) | **Русский**

`Decorations` закрашивает куски текстовой карты кистями: найденное, изменённые строки, комментарии —
всё, что отмечает приложение. Декорации рисуются под текстом и под выделением.

![Подсвеченные найденные слова рядом с выделением](images/selection.png)

## Подсветка найденного

[Текстовая карта](Selection-ru#позиции-текстовая-карта) — это текст, который видит пользователь,
весь документ вместе с блоками за краем, поэтому поиск — обычный `IndexOf` по `TextMap.Text`.

**`MainWindow.xaml`**:

```xml
<DockPanel>
    <TextBox x:Name="SearchBox" DockPanel.Dock="Top" TextChanged="OnSearchChanged" />
    <ScrollViewer>
        <mdv:MarkdownViewer x:Name="Viewer" Markdown="{Binding Text}" Margin="24,16" />
    </ScrollViewer>
</DockPanel>
```

**`MainWindow.xaml.cs`**, в классе окна (`using System.Windows.Media; using MarkdigWpfViewer.Selection;`):

```csharp
private static readonly Brush FoundBrush = new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xC1, 0x07));

private void OnSearchChanged(object sender, TextChangedEventArgs e)
{
    var query = SearchBox.Text;
    var text = Viewer.TextMap.Text;
    var found = new List<MdDecoration>();

    for (var index = query.Length == 0 ? -1 : text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
         index >= 0;
         index = text.IndexOf(query, index + query.Length, StringComparison.OrdinalIgnoreCase))
    {
        found.Add(new MdDecoration(new MdTextRange(index, query.Length), FoundBrush));
    }

    Viewer.Decorations = found;
    if (found.Count > 0)
    {
        Viewer.ScrollToRange(found[0].Range, 0.3);
    }
}
```

Чтобы ходить по результатам, держат номер текущего, прокручивают к нему `ScrollToRange` и дают ему
свою кисть.

## Другие куски

Декорация — любой кусок текстовой карты с любой кистью. `TextMap.RangeOfSourceLines(start, end)`
превращает строки исходника в кусок — так [две панели](Source-Lines-and-Two-Panes-ru) подсвечивают
изменённые строки. Кисти делают полупрозрачными: текст рисуется поверх них.

## Когда текст меняется

Новый `Markdown` сразу строит новую текстовую карту, и старые позиции могут указывать уже не туда.
Декорации пересчитывают сразу после того, как задан новый текст.
