[English](Application-Links) | **Русский**

Ссылка может вести не в веб, а внутрь приложения: `[the plan](note:plan-2026)` открывает заметку,
`[Anna](user:anna)` — профиль. Прежде чем что-то делать со щелчком, вьюер спрашивает приложение, а
приложение может ещё и заменить то, что происходит со ссылками, которые никто не обработал. Весь
порядок щелчка — в [Ссылках и якорях](Links-and-Anchors-ru#что-делает-щелчок).

## Событие Navigate

`MarkdownLink.Navigate` — всплывающее маршрутизируемое событие: обработчик на вьюере или любом его
предке получает каждый щелчок по ссылке, картинке-ссылке, ссылке на сноску или `↩` — и по ссылке
[виджета расширения](Extensions-ru#ссылка-из-виджета). Обработанное событие на этом заканчивается.

**`MainWindow.xaml`**:

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}" mdv:MarkdownLink.Navigate="OnMarkdownLink" />
```

**`MainWindow.xaml.cs`**, в классе окна:

```csharp
private void OnMarkdownLink(object sender, MarkdownLinkEventArgs e)
{
    if (!e.Href.StartsWith("note:", StringComparison.Ordinal))
    {
        return;
    }

    MessageBox.Show($"Open the note {e.Href["note:".Length..]}");
    e.Handled = true;
}
```

Чужая ссылка остаётся необработанной и идёт дальше как обычно: якоря прокручивают, ссылки наружу
открываются.

Цель ссылки приходит дважды: `Href` — строка ровно так, как она записана в Markdown, `Uri` — её
разбор. Для своих схем нужен `Href`: схема с подчёркиванием вроде `my_app:...` незаконна для URI, и
такой адрес разбирается в относительный `Uri`. Якоря приходят как `#id`, сноски — как `#fn:1` и
`#fnref:1`, так что событием можно взять на себя и переходы внутри документа.

## Команда вместо обработчика

С MVVM подключают команду: она получает `Href` параметром. Команда, у которой `CanExecute(href)`
вернул `false`, оставляет событие необработанным — так же, как обработчик, который вышел.

**`MainWindow.xaml`**:

```xml
<mdv:MarkdownViewer Markdown="{Binding Text}" mdv:MarkdownLink.Command="{Binding OpenLinkCommand}" />
```

**Своя модель представления**, с любой реализацией `ICommand` — здесь `RelayCommand<string>` из
CommunityToolkit.Mvvm:

```csharp
public MainViewModel()
{
    OpenLinkCommand = new RelayCommand<string>(
        href => OpenNote(href!["note:".Length..]),
        href => href?.StartsWith("note:", StringComparison.Ordinal) == true);
}

public ICommand OpenLinkCommand { get; }
```

`OpenNote` — заместитель своего кода.

## Свой открыватель ссылок

Ссылки, которые никто не обработал, получает открыватель ссылок — по умолчанию `MdSystemLinkOpener`:
он открывает адреса веба и почты системным обработчиком, остальное пропускает. Его заменяют, чтобы
поменять это для всех ссылок разом: спрашивать перед уходом из приложения, открывать файлы,
записывать щелчки. Открыватель — наследуемое вложенное свойство `MarkdownLink.Opener`: его ставят на
вьюер, на окно или стилем.

**`ConfirmingLinkOpener.cs`**:

```csharp
using System.Windows;
using MarkdigWpfViewer;

public sealed class ConfirmingLinkOpener : IMdLinkOpener
{
    public void Open(string href, Uri? uri)
    {
        if (MessageBox.Show($"Open {href}?", "Link", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
            MdSystemLinkOpener.Instance.Open(href, uri);
        }
    }
}
```

**`MainWindow.xaml.cs`**, в конструкторе после `InitializeComponent()`; действует на все вьюеры окна:

```csharp
MarkdownLink.SetOpener(this, new ConfirmingLinkOpener());
```

`mdv:MarkdownLink.Opener="{x:Null}"` на вьюере значит, что ни одна его ссылка не уходит из
приложения; событие `Navigate` и якоря продолжают работать.
