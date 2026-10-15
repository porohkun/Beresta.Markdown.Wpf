# Markdig.Wpf.Viewer v0.4.0

Markdown viewer for WPF based on Markdig.

## Прокрутка

Своей прокрутки у вьюера нет: его ставят в `ScrollViewer`, в общую прокрутку страницы рядом с
другим содержимым или в список, в том числе виртуализированный. Вьюер создаёт только блоки,
попавшие в видимую область прокруток вокруг него; без прокрутки создаются все блоки.

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
| Блок кода | `Md.CodeBlock.FontFamily`, `.FontSize`, `.Foreground`, `.Background`, `.BorderBrush`, `.BorderThickness`, `.Padding`, `.CornerRadius` |
| Цитата | `Md.Quote.BorderBrush`, `.BorderThickness`, `.Padding`, `.Margin` |
| Список | `Md.List.Margin`, `Md.List.MarkerWidth`, `Md.List.ItemSpacing` |
| Таблица | `Md.Table.BorderBrush`, `.BorderThickness`, `.CellPadding`, `.HeaderBackground` |
| Разделитель и пустая строка | `Md.HorizontalRule.Margin`, `Md.BlankLine.Height` |
| Начертания | `Md.Strong.Style`, `Md.Emphasis.Style`, `Md.Strike.Style` (стили `Span`), `Md.Link.Style` (стиль `Hyperlink`) |
| Стили блоков целиком | `Md.Paragraph.Style`, `Md.Heading.Style`, `Md.CodeBlock.Style`, `Md.Quote.Style`, `Md.List.Style`, `Md.List.MarkerStyle`, `Md.Table.Style` |

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
