[English](Styles-and-Templates) | **Русский**

Вид вьюера меняется на трёх уровнях, от малого к большому: [ключи темы](Theming-ru) для отдельных
значений, стили для начертаний и блоков целиком, шаблоны для блоков, нарисованных по-своему. Бери
самый маленький уровень, которого хватает.

## Начертания

Жирный, курсив, зачёркнутый и ссылки оформляются стилями `Span` и `Hyperlink` с ключами
`Md.Strong.Style`, `Md.Emphasis.Style`, `Md.Strike.Style` и `Md.Link.Style`.

**`App.xaml`**, ресурсы приложения:

```xml
<Style x:Key="Md.Strong.Style" TargetType="Span">
    <Setter Property="FontWeight" Value="Bold" />
    <Setter Property="Foreground" Value="#B35900" />
</Style>

<Style x:Key="Md.Link.Style" TargetType="Hyperlink">
    <Setter Property="TextDecorations" Value="{x:Null}" />
</Style>
```

## Стили блоков

У каждого контрола блока есть ключ стиля; его перекрывают, чтобы поменять шаблон контрола и оставить
остальную тему.

| Ключ | Тип | Что рисует |
|---|---|---|
| `Md.Paragraph.Style` | `p:MdTextBlock` | абзацы и ячейки таблиц |
| `Md.Heading.Style` | `p:MdHeadingBlock` | заголовки, с триггерами по `Level` |
| `Md.CodeBlock.Style` | `p:CodeBlockPresenter` | блоки кода |
| `Md.CodeBlock.HeaderStyle` | `TextBlock` | шапку языка блока кода |
| `Md.CodeBlock.CopyButtonStyle` | `Button` | кнопку копирования блока кода |
| `Md.Quote.Style` | `p:QuotePresenter` | цитаты |
| `Md.List.Style` | `ItemsControl` | списки |
| `Md.List.MarkerStyle` | `TextBlock` | маркеры списков |
| `Md.Table.Style` | `p:MdTablePresenter` | таблицы |
| `Md.Paragraph.InlineCodeStyle`, `Md.Heading1.InlineCodeStyle` … `Md.Heading6.InlineCodeStyle` | `p:InlineCodePresenter` | код в строке абзацев и заголовков |
| `Md.Kbd.Style` | `p:InlineCodePresenter` | клавиши, `<kbd>` |
| `Md.Image.Style` | `p:MdImage` | картинки |
| `Md.FootnoteLink.Style` | `p:MdFootnoteLink` | ссылки на сноски и `↩` |
| `Md.Footnote.MarkerStyle` | `TextBlock` | номер в определении сноски |

Цитата на подкрашенном фоне вместо черты слева:

**`App.xaml`**, ресурсы приложения:

```xml
<Style x:Key="Md.Quote.Style" TargetType="p:QuotePresenter">
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="p:QuotePresenter">
                <Border Background="#FFF8E1" CornerRadius="4" Padding="12,8" Margin="0,0,0,4">
                    <p:MdBlocksPresenter Blocks="{TemplateBinding Blocks}" />
                </Border>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>
```

## Шаблоны блоков

Шаблон заменяет то, как рисуется вид блока. Шаблоны держит `MdStyleProvider`; с `BasedOn` на
стандартный провайдер задаются только заменяемые шаблоны, остальные остаются стандартными.

![Свой шаблон заголовка](images/custom-template.png)

**`App.xaml`**, ресурсы приложения:

```xml
<s:MdStyleProvider x:Key="MyStyleProvider" BasedOn="{StaticResource Md.DefaultStyleProvider}">
    <s:MdStyleProvider.HeadingTemplate>
        <DataTemplate DataType="{x:Type vm:HeadingVm}">
            <Border Margin="0,12,0,4" Padding="10,0,0,0" BorderThickness="4,0,0,0" BorderBrush="#326CF3">
                <p:MdHeadingBlock Level="{Binding Level}"
                                  Runs="{Binding Inlines}"
                                  Style="{DynamicResource Md.Heading.Style}"
                                  FontFamily="Georgia"
                                  Margin="0" />
            </Border>
        </DataTemplate>
    </s:MdStyleProvider.HeadingTemplate>
</s:MdStyleProvider>
```

**Окно или пользовательский контрол**:

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}" StyleProvider="{StaticResource MyStyleProvider}" />
```

Всем вьюерам окна провайдер задают стилем на основе собственного стиля вьюера.

**`MainWindow.xaml`**, ресурсы окна:

```xml
<Style TargetType="mdv:MarkdownViewer" BasedOn="{StaticResource {x:Type mdv:MarkdownViewer}}">
    <Setter Property="StyleProvider" Value="{StaticResource MyStyleProvider}" />
</Style>
```

### Что получает шаблон

Контекст данных шаблона — модель его блока, неизменяемая запись:

| Свойство провайдера | Модель | Члены модели |
|---|---|---|
| `ParagraphTemplate` | `vm:ParagraphVm` | `Inlines` |
| `HeadingTemplate` | `vm:HeadingVm` | `Level`, `Inlines`, `Id` |
| `CodeBlockTemplate` | `vm:CodeBlockVm` | `InfoString`, `Syntax`, `Code`, `Runs`, `IsFenced` |
| `QuoteTemplate` | `vm:QuoteVm` | `Blocks` |
| `ListTemplate` | `vm:ListVm` | `Ordered`, `Start`, `Depth`, `Items` из `vm:ListItemVm` (`MarkerText`, `Blocks`) |
| `TableTemplate` | `vm:TableVm` | `ColumnCount`, `Rows`, `Alignments` |
| `HorizontalRuleTemplate` | `vm:ThematicBreakVm` | — |
| `BlankLineTemplate` | `vm:BlankLineVm` | — |
| `FootnoteTemplate` | `vm:FootnoteVm` | `Order`, `Label`, `Blocks` |
| `ImageTemplate` | `vm:ImageVm`, строчный | `Url`, `Alt`, `Title`, `Width`, `Height`, `Href` |
| `TaskTemplate` | `vm:TaskVm`, строчный | `IsChecked` |
| `FootnoteLinkTemplate` | `vm:FootnoteLinkVm`, строчный | `Order`, `Index`, `IsBack`, `Target` |

Стандартные шаблоны лежат в
[`Themes/Generic.xaml`](https://github.com/porohkun/Markdig.Wpf.Viewer/blob/main/Markdig.Wpf.Viewer/Themes/Generic.xaml)
под ключом `Md.DefaultStyleProvider` — их удобно брать за основу.

### Чтобы выделение работало

- Текст рисуют `p:MdTextBlock Runs="{Binding Inlines}"` (для заголовков — `p:MdHeadingBlock`): в нём
  работают выделение, подсветка, ссылки и строчные виджеты. Блок, который рисует текст иначе,
  выделяется только целиком.
- Вложенные блоки рисуют `p:MdBlocksPresenter Blocks="{Binding Blocks}"`: они получают те же шаблоны
  и ту же виртуализацию, что документ.
- Свой шаблон со своим `ScrollViewer` отдаёт колесо мыши странице через
  `b:NestedScroll.PassWheelToParent="True"` — см. [Прокрутку и виртуализацию](Scrolling-and-Virtualization-ru#вложенная-прокрутка).

## Шаблон вьюера

Если заменить шаблон самого `MarkdownViewer`, в нём должны остаться `ItemsPresenter` для блоков и
`p:MdLineGutter x:Name="PART_Gutter"` для [номеров строк](Line-Numbers-ru).
