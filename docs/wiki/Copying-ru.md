[English](Copying) | **Русский**

Ctrl+C, контекстное меню и `Copy()` кладут выделенное в буфер обмена сразу в трёх форматах, а
программа, куда вставляют, берёт понятный ей.

| Формат | Что в нём | Кто берёт |
|---|---|---|
| `DataFormats.UnicodeText` | обычный текст, как на экране | Блокнот, терминалы, простые поля ввода |
| `MdDataFormats.Markdown` (`text/markdown`) | разметка выделенного, байты UTF-8 | свои приложения |
| `DataFormats.Html` | выделенное в HTML | Word, почта, редакторы с форматированием |

Разметка записывается заново из модели документа. Кусок одной строки сохраняет начертания и ссылки;
несколько блоков — ещё и заголовки, списки, цитаты, блоки кода и таблицы. Ссылки по метке становятся
обычными, а `_курсив_` — `*курсивом*`. Виджет, выделенный целиком, записывается своим исходным
текстом.

**Свой код, который вставляет** (`using System.IO; using System.Text; using MarkdigWpfViewer.Selection;`):

```csharp
if (Clipboard.GetData(MdDataFormats.Markdown) is MemoryStream stream)
{
    var markdown = Encoding.UTF8.GetString(stream.ToArray());
}
```

## Копировать как Markdown

Ctrl+Shift+C, пункт меню «Copy as Markdown» и `CopyMarkdown()` кладут разметку выделенного обычным
текстом: её можно вставить в любое поле, и оформление сохранится разметкой. За ними стоит команда
`MarkdownViewer.CopyMarkdownCommand` — ею может пользоваться и своя кнопка.

**Окно или пользовательский контрол**:

```xml
<Button Content="Copy as Markdown"
        Command="{x:Static mdv:MarkdownViewer.CopyMarkdownCommand}"
        CommandTarget="{Binding ElementName=Viewer}" />
```

Без буфера обмена те же тексты лежат в `SelectedText` и `SelectedMarkdown`.

## Своё в копировании

Перед записью в буфер вьюер поднимает стандартное событие `DataObject.Copying`. Обработчик на вьюере
или любом его предке получает данные со всеми тремя форматами: может дописать свой формат или
отменить копирование. Кнопка копирования блока кода поднимает то же событие.

**`MainWindow.xaml.cs`**, в конструкторе после `InitializeComponent()`:

```csharp
DataObject.AddCopyingHandler(Viewer, (_, e) =>
{
    if (IsDocumentProtected)
    {
        e.CancelCommand();
        return;
    }

    e.DataObject.SetData("YourApp.DocumentId", DocumentId);
});
```

`IsDocumentProtected` и `DocumentId` — заместители своего состояния.

## Ctrl+C отдаётся приложению

С `IsCopyEnabled="False"` вьюер не берёт команду `Copy`: Ctrl+C уходит к своей привязке команды, и
что класть в буфер, решает приложение. `Copy()` и `CopyMarkdown()`, вызванные из кода, работают.

**`MainWindow.xaml`**:

```xml
<Window.CommandBindings>
    <CommandBinding Command="Copy" CanExecute="OnCanCopy" Executed="OnCopy" />
</Window.CommandBindings>

<ScrollViewer>
    <mdv:MarkdownViewer x:Name="Viewer" Markdown="{Binding Text}" IsCopyEnabled="False" />
</ScrollViewer>
```

**`MainWindow.xaml.cs`**, в классе окна:

```csharp
private void OnCanCopy(object sender, CanExecuteRoutedEventArgs e)
    => e.CanExecute = Viewer.SelectionLength > 0;

private void OnCopy(object sender, ExecutedRoutedEventArgs e)
    => Clipboard.SetText(Viewer.SelectedMarkdown);
```

## Контекстное меню

По умолчанию у вьюера меню из «Copy», «Copy as Markdown» и «Select All». Подписи — ключи темы,
переводятся, как описано в [Теме](Theming-ru#подписи-интерфейса).

Своё меню — обычное свойство `ContextMenu`. Команды направляют во вьюер через `CommandTarget`:
иначе они уходят элементу с фокусом клавиатуры.

**`MainWindow.xaml`**:

```xml
<mdv:MarkdownViewer x:Name="Viewer" Markdown="{Binding Text}">
    <mdv:MarkdownViewer.ContextMenu>
        <ContextMenu>
            <MenuItem Command="Copy"
                      CommandTarget="{Binding PlacementTarget, RelativeSource={RelativeSource AncestorType=ContextMenu}}" />
            <MenuItem Header="Search the web" Click="OnSearchWeb" />
        </ContextMenu>
    </mdv:MarkdownViewer.ContextMenu>
</mdv:MarkdownViewer>
```

**`MainWindow.xaml.cs`**, в классе окна (`using System.Diagnostics;`):

```csharp
private void OnSearchWeb(object sender, RoutedEventArgs e)
{
    var query = Uri.EscapeDataString(Viewer.SelectedText);
    Process.Start(new ProcessStartInfo($"https://duckduckgo.com/?q={query}") { UseShellExecute = true });
}
```

Чтобы поменять меню всем вьюерам, в ресурсах приложения определяют свой ресурс с ключом
`Md.ContextMenu` и `x:Shared="False"`. `ContextMenu="{x:Null}"` на вьюере убирает меню.

## Блоки кода

У блока кода своя кнопка копирования, она копирует весь код — см. [Блоки кода](Code-Blocks-ru).
