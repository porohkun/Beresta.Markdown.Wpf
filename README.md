# Markdig.Wpf.Viewer v0.4.0

Markdown viewer for WPF based on Markdig.

## Прокрутка

Своей прокрутки у вьюера нет: его ставят в `ScrollViewer`, в общую прокрутку страницы рядом с
другим содержимым или в список, в том числе виртуализированный. Вьюер создаёт только блоки,
попавшие в видимую область прокруток вокруг него; без прокрутки создаются все блоки.

## Строки как в исходнике

Каждая строка исходника начинается на экране с новой строки: перенос внутри абзаца рисуется
переносом, а не пробелом. Открывающее ограждение блока кода тоже получает свою строку — шапку с
языком.

## Свой вид

Вид меняется снаружи на трёх уровнях: ключами темы, стилями начертаний и шаблонами блоков.

**Ключи темы** перекрываются ресурсами приложения, окна или самого вьюера. Шрифт и цвет текста
по умолчанию не заданы и берутся у приложения — задай ключ, только если текст во вьюере должен
отличаться.

| Что | Ключи |
|---|---|
| Абзац | `Md.Paragraph.FontFamily`, `Md.Paragraph.Foreground` (не заданы), `Md.Paragraph.FontSize`, `Md.Paragraph.LinkForeground` |
| Заголовки | `Md.Heading.FontFamily`, `Md.Heading.Foreground` (не заданы), `Md.Heading.LinkForeground`, `Md.Heading1…6.FontSize`, `.FontWeight`, `.Margin` |
| Код посреди строки | `Md.InlineCode.FontFamily`, `.FontSize`, `.Foreground`, `.Background`, `.BorderBrush`, `.BorderThickness`, `.Padding`, `.CornerRadius` |
| Блок кода | `Md.CodeBlock.FontFamily`, `.FontSize`, `.Foreground`, `.Background`, `.BorderBrush`, `.BorderThickness`, `.Padding`, `.CornerRadius`, `.LineNumberForeground`, `.LineNumberMargin` |
| Шапка блока кода | `Md.CodeBlock.HeaderFontFamily` (не задан), `Md.CodeBlock.HeaderForeground`, `.HeaderFontSize`, `.HeaderMargin` |
| Поля с номерами строк | `Md.Gutter.FontFamily` (не задан), `Md.Gutter.Foreground`, `.FontSize`, `.Padding` |
| Цитата | `Md.Quote.BorderBrush`, `.BorderThickness`, `.Padding`, `.Margin` |
| Список | `Md.List.Margin`, `Md.List.MarkerWidth`, `Md.List.ItemSpacing` |
| Таблица | `Md.Table.BorderBrush`, `.BorderThickness`, `.CellPadding`, `.HeaderBackground` |
| Разделитель и пустая строка | `Md.HorizontalRule.Margin`, `Md.BlankLine.Height` |
| Начертания | `Md.Strong.Style`, `Md.Emphasis.Style`, `Md.Strike.Style` (стили `Span`), `Md.Link.Style` (стиль `Hyperlink`) |
| Стили блоков целиком | `Md.Paragraph.Style`, `Md.Heading.Style`, `Md.CodeBlock.Style`, `Md.CodeBlock.HeaderStyle`, `Md.Quote.Style`, `Md.List.Style`, `Md.List.MarkerStyle`, `Md.Table.Style` |

**Шаблон блока** заменяется своим провайдером: незаданные шаблоны он берёт у стандартного.

```xml
<s:MdStyleProvider x:Key="MyProvider" BasedOn="{StaticResource Md.DefaultStyleProvider}">
    <s:MdStyleProvider.HeadingTemplate>
        <DataTemplate DataType="{x:Type vm:HeadingVm}">
            <Border BorderThickness="4,0,0,0" BorderBrush="Orange" Padding="8,0,0,0">
                <p:MdHeadingBlock Level="{Binding Level}" Runs="{Binding Inlines}"
                                  Style="{DynamicResource Md.Heading.Style}" />
            </Border>
        </DataTemplate>
    </s:MdStyleProvider.HeadingTemplate>
</s:MdStyleProvider>

<mdv:MarkdownViewer StyleProvider="{StaticResource MyProvider}" Markdown="{Binding Text}" />
```

Блоки внутри своего шаблона — цитаты, заметки — рисует `p:MdBlocksPresenter Blocks="{Binding Blocks}"`:
той же цепочкой шаблонов и с той же виртуализацией.

## Свой синтаксис

Расширение — наследник `MdExtension`: настраивает разбор Markdig, превращает свои узлы в виджеты
(записи от `BlockVm` или `InlineVm`) и даёт им шаблоны. Вложенную разметку своего узла
разбирает `MdConvertContext`.

```csharp
public sealed record NoteVm(string Title, IReadOnlyList<BlockVm> Blocks) : BlockVm;

public sealed class NoteExtension : MdExtension
{
    public DataTemplate? Template { get; set; }

    public override BlockVm? ConvertBlock(Block block, MdConvertContext context)
        => block is CustomContainer { Info: "note" } container
            ? new NoteVm(container.Arguments ?? "", context.ConvertChildren(container))
            : null;

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item is NoteVm ? Template : null;
}
```

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}">
    <mdv:MarkdownViewer.Extensions>
        <my:NoteExtension Template="{StaticResource NoteTemplate}" />
    </mdv:MarkdownViewer.Extensions>
</mdv:MarkdownViewer>
```

Расширения опрашиваются по порядку раньше встроенного разбора, поэтому могут заменить и
стандартный блок — например, блок кода с языком `mermaid`. Что не узнало ни одно расширение,
рисуется как обычно; незнакомый контейнер показывает своё содержимое. Общий набор расширений на
несколько вьюеров задаётся свойством `Extensions` в стиле.

**Состояние виджета** — свёрнут ли, что выбрано — живёт дольше его контрола: контрол исчезает,
когда блок уходит из видимого, и пересоздаётся новым текстом. Объект состояния создаёт
расширение, вьюер хранит его по `NodeId`, а шаблон получает через `MdWidget.State`:

```csharp
public override object? CreateState(NodeVm node) => node is NoteVm ? new NoteState() : null;
```

```xml
<ToggleButton IsChecked="{Binding (mdx:MdWidget.State).IsExpanded, RelativeSource={RelativeSource Self}}" />
```

Виджет может менять высоту и после загрузки — картинка, диаграмма: видимое при этом не сдвигается.

## Выделение и подсветка

Текст выделяется мышью сквозь блоки, в том числе кусками блоков; `Ctrl+A` выделяет всё,
`Ctrl+C` копирует — и блоки, которых сейчас нет на экране. Снаружи выделением управляют
`Select(start, length)`, `SelectAll()`, `ClearSelection()`, а читают его `SelectionStart`,
`SelectionLength`, `SelectedText` и событие `SelectionChanged`. Позиции — в текстовой карте
`TextMap`: в её сквозном тексте `TextMap.Text` и ищут.

`Decorations` подсвечивает свои куски карты под выделением — так рисуются найденное или правки:

```csharp
viewer.Decorations = matches
    .Select(match => new MdDecoration(new MdTextRange(match.Index, match.Length), FoundBrush))
    .ToList();
```

Цвет выделения — ключ темы `Md.Selection.Brush`. Виджет, который реализует `IMdTextSource`, отдаёт
в карту свой текст и вложенные блоки; без него он неделимый и выделяется целиком, а копируется
текстом из `IMdCopyText`, если реализует его. Внутри элемента с `MdSelection.IsInteractive="True"`
выделение не начинается — кнопки и поля ввода узнаются и так.

## Строки исходника и две панели

Вьюер переводит строки исходника в позиции на экране и обратно — этого хватает, чтобы
синхронизировать его с редактором или со вторым вьюером. Строки считаются с нуля, дробная часть
— доля строки.

| Что | Как |
|---|---|
| Где строка | `GetSourceLineOffset(line)` — позиция в координатах вьюера |
| Какая строка в позиции | `GetSourceLineAt(offset)` |
| Что сейчас видно | `GetVisibleSourceLine(fraction)` — строка в доле высоты прокрутки; событие `ViewportChanged` |
| Прокрутить к строке | `ScrollToSourceLine(line, fraction)` — строка встаёт в долю высоты прокрутки |
| Подсветить строки | `TextMap.RangeOfSourceLines(start, end)` → кусок для `Decorations` |

Две панели с разными текстами, как в диффе, синхронизирует `MdScrollSync`. Соответствие строк
задаёт `MdLineMap` по опорным парам — например, по неизменённым строкам; между опорами строки
пересчитываются линейно. Ведущая панель — та, с которой работает пользователь.

```csharp
var anchors = unchanged.SelectMany(pair => new[]
{
    ((double)pair.Left, (double)pair.Right),
    (pair.Left + 1.0, pair.Right + 1.0)
});
var sync = new MdScrollSync(leftViewer, rightViewer, new MdLineMap(anchors));
```

Прокрутка вокруг вьюера должна быть попиксельной: логическая прокрутка списков сдвигает
элементами, и строку в нужную точку так не поставить.

## Номера строк

`ShowSourceLineNumbers` показывает слева поля с номерами строк исходника — у каждой строки, у
которой есть своё место на экране: строки текста, пустые строки, шапка блока кода и его строки.
Закрывающее ограждение блока кода, разделитель таблицы и подчёркивание заголовка своего места не
имеют и номера не получают. Ширина полей общая на документ. `ShowCodeLineNumbers` нумерует строки
внутри блоков кода; свойство наследуется, поэтому его задают и один раз на окно или список
вьюеров. Номера показываются с единицы, как в редакторе.

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}" ShowSourceLineNumbers="True" />

<ItemsControl mdv:MarkdownViewer.ShowCodeLineNumbers="True" ItemsSource="{Binding Messages}" />
```

Свой шаблон вьюера сохраняет поля, если в нём есть `p:MdLineGutter x:Name="PART_Gutter"`.

## Новый текст

Новый `Markdown` ложится на показанный документ правками, так что вьюер годится для живого
предпросмотра и потоковых ответов. Блоки, которые не изменились или изменились на своём месте,
остаются в своих контейнерах вместе с состоянием виджетов, известные высоты не сбрасываются, а
видимое не уезжает, когда текст меняется выше него.

У каждого элемента модели есть `NodeId` — он переживает новый текст, пока элемент не изменился
или изменился на своём месте. Вид виджета переживает правку, если расширение отдаёт для него один
и тот же `DataTemplate`, а не создаёт новый на каждый вызов.

## Ссылки

По умолчанию нажатая ссылка на веб или почту (`http`, `https`, `mailto`, а также `www.…` без
схемы) открывается системным обработчиком; ссылки с другими схемами и пути к файлам наружу не
уходят. Но сначала вьюер спрашивает приложение: поднимает всплывающее `MarkdownLink.Navigate`, и
обработанное событие внешнее открытие отменяет.

Это и есть точка для своих ссылок вида `[подпись](своя_схема:идентификатор)`. Подключается она
двумя способами, оба — на вьюер или любого его предка:

```xml
<!-- обработчиком -->
<mdv:MarkdownViewer Markdown="{Binding Text}"
                    mdv:MarkdownLink.Navigate="OnMarkdownLink" />

<!-- командой; параметр — Href -->
<mdv:MarkdownViewer Markdown="{Binding Text}"
                    mdv:MarkdownLink.Command="{Binding OpenLinkCommand}" />
```

```csharp
private void OnMarkdownLink(object sender, MarkdownLinkEventArgs e)
{
    if (!MyId.TryParse(e.Href, out var id))
        return;         // не наша ссылка — пусть открывается наружу

    Open(id);
    e.Handled = true;   // наша — наружу не отдаём
}
```

Цель ссылки приходит дважды: `Href` — строка ровно из markdown, `Uri` — её разбор. Для своих
схем нужен `Href`: схема с подчёркиванием (`codex_page:...`) по RFC 3986 незаконна, и такой
адрес разбирается в относительный `Uri`. Команда, у которой `CanExecute(href)` вернул `false`,
событие не гасит — ссылка уходит наружу, как если бы команды не было вовсе.
