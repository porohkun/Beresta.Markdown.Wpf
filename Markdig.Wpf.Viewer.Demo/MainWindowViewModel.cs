namespace MarkdigWpfViewer.Demo;

internal class MainWindowViewModel
{
    public string Text { get; } =
        @"# Заголовок 1
## Заголовок 2
### Заголовок 3
#### Заголовок 4  и `код`
##### Заголовок 5
###### Заголовок 6

Просто текст!, **Жирный**, *Наклонный*, ~~Зачеркнутый~~, `код`.

Ссылка наружу — [на гитхаб](https://github.com/porohkun/Markdig.Wpf.Viewer), ссылка внутрь —
[своя схема](demo:42): первая откроется браузером, вторую перехватит окно.

---

- список
- из
- пунктов
	- с
	- идентацией
		- глубже
			- еще глубже
	- назад
		- вперед
- еще один пункт, очнь длинный пункт, настолько длинный, что обязательно должен перейти на новую строчку, чтобы проверить, как работает перенос в списке.

1. нумерованный
2. список
	1. тоже
	2. очень
	3. нужен
3. и
4. важен

- а как насчет
	1. нумерованного
	2. в обычном
-  а? а? А?

- или
- другой
- вариант
  1. о как
  2. неплохо?
     - и вот так
     - как **тебе** *такое* ~~илон~~ `маск?`
> - возможно
> - без
> - отступов
> - но с `кодом`

| а вот      | и            | таблица!                     |
| ---------- | ------------ | ---------------------------- |
| опа        | жопа         | труляля                      |
| кики       | пики         | но `не дики`                 |
| **жирный** | *жирный*     | ~~жирный~~                   |
| ***как***  | *поезд*      | ***~~пассажирный~~***        |

```
public class ChatControl : Control
{
    public static readonly DependencyProperty ChatProperty =
        DependencyProperty.Register(nameof(Chat), typeof(ChatMessagesViewModel), typeof(ChatControl), new(null, ChatChanged));

    public ChatMessagesViewModel? Chat
    {
        get => (ChatMessagesViewModel)GetValue(ChatProperty);
        set => SetValue(ChatProperty, value);
    }
}
```
";
}