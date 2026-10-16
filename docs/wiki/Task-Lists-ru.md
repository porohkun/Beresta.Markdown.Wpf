[English](Task-Lists) | **Русский**

Пункт списка, который начинается с `[ ]` или `[x]`, получает флажок вместо этого текста. По
умолчанию флажок только показывает состояние. Сам вьюер текст не меняет никогда: щелчок по
включённому флажку просит сделать это приложение.

**Любой `.md`-файл**:

```markdown
- [x] take screenshots
- [ ] write the docs
```

## Переключение задач

`CanToggleTasks="True"` включает флажки. Это наследуемое вложенное свойство: его ставят на вьюер или
один раз на окно или список вьюеров. Щелчок поднимает всплывающее маршрутизируемое событие
`TaskToggled` с моделью задачи `Task` и желаемым состоянием `IsChecked`. `Task.Source` — место `[ ]` в
исходнике, так что приложение переписывает эти три знака, и новый текст обновляет флажок.

**`MainWindow.xaml`**:

```xml
<ScrollViewer>
    <mdv:MarkdownViewer x:Name="Viewer"
                        CanToggleTasks="True"
                        TaskToggled="OnTaskToggled"
                        Margin="24,16" />
</ScrollViewer>
```

**`MainWindow.xaml.cs`**, в классе окна:

```csharp
public MainWindow()
{
    InitializeComponent();
    Viewer.Markdown = "- [x] take screenshots\n- [ ] write the docs";
}

private void OnTaskToggled(object? sender, MdTaskToggledEventArgs e)
{
    var text = Viewer.Markdown ?? string.Empty;
    var source = e.Task.Source;
    Viewer.Markdown = text[..source.Start] + (e.IsChecked ? "[x]" : "[ ]") + text[source.End..];
}
```

С привязкой новый текст пишут в свою модель представления, а не в `Viewer.Markdown`: прямое
присваивание заменит привязку. Текст ложится правками, поэтому остальной документ не двигается — см.
[Живой предпросмотр](Live-Preview-ru).

## Копирование и вид

Задача копируется как `[x]` или `[ ]` в обычном тексте и как `- [x]` в Markdown. Флажок берёт стиль
`CheckBox` приложения; его отступ — ключ `Md.Task.Margin`, а `TaskTemplate` провайдера стилей
заменяет его — см. [Стили и шаблоны](Styles-and-Templates-ru).

## Статусы задач

Статусы Obsidian `[/]`, `[-]`, `[>]`, `[?]` и `[!]` даёт [готовое расширение](Kit-Text-ru#статусы-задач)
`TaskStatuses`: значок вместо флажка и тот же `TaskToggled` по щелчку, где `Task.Status` — знак статуса.
