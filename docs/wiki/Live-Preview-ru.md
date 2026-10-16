[English](Live-Preview) | **Русский**

Новое значение `Markdown` не пересобирает документ: вьюер сопоставляет новый документ с показанным
и применяет разницу правками. Блоки, которые не изменились или изменились на своём месте, сохраняют
контролы, измеренные высоты и состояние виджетов; выделение остаётся, пока живы его блоки; видимое
не уезжает, когда текст меняется выше него. Поэтому вьюер годится и для предпросмотра рядом с
редактором, и для ответов, которые приходят по кускам.

## Предпросмотр рядом с редактором

**`MainWindow.xaml`**:

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition />
        <ColumnDefinition />
    </Grid.ColumnDefinitions>

    <TextBox x:Name="Editor"
             AcceptsReturn="True"
             AcceptsTab="True"
             TextWrapping="Wrap"
             VerticalScrollBarVisibility="Auto"
             FontFamily="Consolas" />

    <ScrollViewer Grid.Column="1">
        <mdv:MarkdownViewer Markdown="{Binding Text, ElementName=Editor}" Margin="24,16" />
    </ScrollViewer>
</Grid>
```

Вьюер получает каждое нажатие клавиши. Каждая правка разбирает весь текст, поэтому для очень длинных
документов `Markdown` лучше обновлять после короткой паузы в наборе.

## Ответ по кускам

Ответ, который приходит кусками, показывают, присваивая после каждого куска весь текст, пришедший к
этому моменту. Меняется только последний блок, остальное стоит как было.

**`MainWindow.xaml.cs`**, обработчик кнопки; куски здесь берутся из строки (`using System.Text;`):

```csharp
private async void OnAnswer(object sender, RoutedEventArgs e)
{
    const string answer = "## Answer\n\nThe viewer applies **every new text** as edits, so a streamed answer does not flicker.";

    var shown = new StringBuilder();
    foreach (var word in answer.Split(' '))
    {
        shown.Append(word).Append(' ');
        Viewer.Markdown = shown.ToString();
        await Task.Delay(40);
    }
}
```

## Устойчивые идентификаторы

У каждого элемента модели есть `NodeId` — устойчивый идентификатор, который переживает новый текст,
пока элемент не изменился или изменился на своём месте. По нему вьюер хранит контролы и
[состояние виджетов](Extensions-ru#состояние-виджета). Чтобы виджет сохранял контрол, его расширение
должно каждый раз возвращать один и тот же экземпляр `DataTemplate`.
