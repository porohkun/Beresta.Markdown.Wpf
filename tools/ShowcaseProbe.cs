#:project ../Beresta.Markdown.Wpf.Showcase/Beresta.Markdown.Wpf.Showcase.csproj
#:property TargetFramework=net10.0-windows
#:property UseWPF=true
#:property PublishAot=false
#:property PublishTrimmed=false

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Beresta.Markdown.Wpf;
using Beresta.Markdown.Wpf.Editor;
using Beresta.Markdown.Wpf.Presenters;
using Beresta.Markdown.Wpf.Scrolling;
using Beresta.Markdown.Wpf.Selection;
using Beresta.Markdown.Wpf.ViewModels;
using Beresta.Markdown.Wpf.Showcase;
using Beresta.Markdown.Wpf.Showcase.Localization;
using Beresta.Markdown.Wpf.Showcase.Showcases.Copying;
using Beresta.Markdown.Wpf.Showcase.Showcases.CustomLook;
using Beresta.Markdown.Wpf.Showcase.Showcases.Editor;
using Beresta.Markdown.Wpf.Showcase.Showcases.Sets;
using Beresta.Markdown.Wpf.Showcase.Showcases.TwoPanes;
using ColorPicker = HandyControl.Controls.ColorPicker;
using NumericUpDown = HandyControl.Controls.NumericUpDown;
using PropertyItem = HandyControl.Controls.PropertyItem;

const int ExtendedStyle = -20;
const int NoActivate = 0x08000000;
const int ToolWindow = 0x00000080;
const double SyncAnchor = 0.38;

if (args.Length == 0)
{
    Console.WriteLine("dotnet run tools/ShowcaseProbe.cs -- <шаги через запятую> [папка снимков]");
    return;
}

var steps = args[0].Split(',');
var output = args.Length > 1 ? args[1] : Path.Combine(Path.GetTempPath(), "ShowcaseProbe");
Directory.CreateDirectory(output);

var thread = new Thread(() =>
{
    var app = new App();
    app.InitializeComponent();

    // BYDESIGN: запуск по StartupUri снят в обход сеттера, который не берёт null: конструктор Application ставит
    // запуск в очередь, и первый же её разбор открыл бы на экране вторую витрину, обычную и с фокусом.
    typeof(Application).GetField("_startupUri", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(app, null);
    app.ShutdownMode = ShutdownMode.OnExplicitShutdown;

    // BYDESIGN: окно за экраном и не становится активным: не мелькает перед пользователем и не забирает его ввод.
    var window = new MainWindow
    {
        Left = -10000,
        Top = -10000,
        ShowActivated = false,
        ShowInTaskbar = false,
        WindowStartupLocation = WindowStartupLocation.Manual
    };
    window.SourceInitialized += (_, _) =>
    {
        var handle = new WindowInteropHelper(window).Handle;
        SetWindowLong(handle, ExtendedStyle, GetWindowLong(handle, ExtendedStyle) | NoActivate | ToolWindow);
    };
    window.Show();
    Settle(window);

    var model = (MainWindowViewModel)window.DataContext;
    var shot = 0;
    var errors = new Dictionary<string, int>();
    var bindingErrors = new CountingListener(errors);
    Border? probe = null;
    foreach (var step in steps)
    {
        var colon = step.IndexOf(':');
        var (name, value) = colon < 0 ? (step, string.Empty) : (step[..colon], step[(colon + 1)..]);
        switch (name)
        {
            // Витрина и общие настройки
            case "tab":
            {
                model.Selected = int.TryParse(value, out var index)
                    ? model.Showcases[index]
                    : model.Showcases.Single(showcase => showcase.Title == value);
                break;
            }
            case "light":
                model.Options.IsDark = false;
                break;
            case "dark":
                model.Options.IsDark = true;
                break;
            case "lang":
                model.Options.IsRussian = value == "ru";
                break;
            case "gutter":
                model.Options.ShowSourceLineNumbers = !model.Options.ShowSourceLineNumbers;
                break;
            case "codenumbers":
                model.Options.ShowCodeLineNumbers = !model.Options.ShowCodeLineNumbers;
                break;
            case "highlight":
                model.Options.HighlightCode = !model.Options.HighlightCode;
                break;
            case "noselect":
                model.Options.IsSelectionEnabled = false;
                break;
            case "load":
                foreach (var viewer in Viewers(window))
                {
                    viewer.Markdown = File.ReadAllText(value);
                }

                break;
            case "kit":
                foreach (var viewer in Viewers(window))
                {
                    Beresta.Markdown.Wpf.Extensions.MdExtension[] kit =
                    [
                        new Beresta.Markdown.Wpf.Kit.MkDocsAdmonitions(), new Beresta.Markdown.Wpf.Kit.CalloutContainers(),
                        new Beresta.Markdown.Wpf.Kit.WikiEmbeds(), new Beresta.Markdown.Wpf.Kit.Chips(), new Beresta.Markdown.Wpf.Kit.Spoilers()
                    ];
                    foreach (var extension in kit)
                    {
                        viewer.Extensions.Add(extension);
                    }
                }

                break;
            case "wait":
                Pump(int.Parse(value));
                break;

            // Цена передачи набранного во вьюер: разбор, укладка документа, перестройка строк AvalonEdit
            case "commit":
            {
                var editor = Editor(window);
                var watch = Stopwatch.StartNew();
                editor.CommitText();
                var committed = watch.ElapsedMilliseconds;
                editor.UpdateLayout();
                var laid = watch.ElapsedMilliseconds;
                DoEvents();
                Console.WriteLine($"commit: передача {committed} мс, раскладка {laid - committed} мс, очередь диспетчера {watch.ElapsedMilliseconds - laid} мс");
                break;
            }

            // Мигание каретки AvalonEdit без фокуса: blink показывает каретку вызовом, watch — нет. Состояние её
            // слоя по времени (видна, погашена миганием, спрятана), промежутки тиков мигания, долгие операции диспетчера,
            // исключения первого шанса и то, что студия под отладчиком видит дорого: ошибки привязок, отладочный вывод и
            // изменения визуального дерева — с переменной ENABLE_XAML_DIAGNOSTICS_VISUAL_TREE_NOTIFICATIONS=1; у корня
            // шаблона или контейнера — с вьюмоделью и её строкой
            case "blink":
            case "watch":
            {
                var editor = Editor(window);
                var caret = editor.TextEditor.TextArea.Caret;
                if (name == "blink")
                {
                    caret.Show();
                }

                const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
                var layer = typeof(ICSharpCode.AvalonEdit.Editing.Caret).GetField("caretAdorner", Hidden)!.GetValue(caret)!;
                var visible = layer.GetType().GetField("isVisible", Hidden)!;
                var blink = layer.GetType().GetField("blink", Hidden)!;
                var timer = (DispatcherTimer)layer.GetType().GetField("caretBlinkTimer", Hidden)!.GetValue(layer)!;
                var watch = Stopwatch.StartNew();
                var ticks = new List<long>();
                var lines = new List<long>();
                var slow = new List<string>();
                var started = new Dictionary<DispatcherOperation, long>();
                var layouts = 0;
                var thrown = new Dictionary<string, int>();
                EventHandler<System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs> onThrown = (_, e) =>
                {
                    var key = $"{e.Exception.GetType().Name}: {e.Exception.Message}";
                    lock (thrown)
                    {
                        thrown[key] = thrown.GetValueOrDefault(key) + 1;
                    }
                };
                var changes = new Dictionary<string, int>();
                EventHandler<System.Windows.Diagnostics.VisualTreeChangeEventArgs> onTree = (_, e) =>
                {
                    var model = (e.Child as FrameworkElement)?.DataContext;
                    var root = model is NodeVm node && !ReferenceEquals(model, (e.Parent as FrameworkElement)?.DataContext)
                        ? $"[{node.GetType().Name} {node.Source.StartLine}]"
                        : string.Empty;
                    var key = $"{e.ChangeType} {e.Child.GetType().Name}{root} в {e.Parent?.GetType().Name}";
                    changes[key] = changes.GetValueOrDefault(key) + 1;
                };
                var traced = new Dictionary<string, int>();
                var listener = new CountingListener(traced);
                EventHandler onTick = (_, _) => ticks.Add(watch.ElapsedMilliseconds);
                EventHandler onLines = (_, _) => lines.Add(watch.ElapsedMilliseconds);
                EventHandler onLayout = (_, _) => layouts++;
                DispatcherHookEventHandler onStart = (_, e) => started[e.Operation] = watch.ElapsedMilliseconds;
                DispatcherHookEventHandler onDone = (_, e) =>
                {
                    if (started.Remove(e.Operation, out var at) && watch.ElapsedMilliseconds - at > 30)
                    {
                        var method = typeof(DispatcherOperation).GetField("_method", Hidden)?.GetValue(e.Operation) as Delegate;
                        slow.Add($"{at}+{watch.ElapsedMilliseconds - at} {e.Operation.Priority} {method?.Method.DeclaringType?.Name}.{method?.Method.Name}");
                    }
                };
                timer.Tick += onTick;
                editor.TextEditor.TextArea.TextView.VisualLinesChanged += onLines;
                window.LayoutUpdated += onLayout;
                AppDomain.CurrentDomain.FirstChanceException += onThrown;
                if (Environment.GetEnvironmentVariable("ENABLE_XAML_DIAGNOSTICS_VISUAL_TREE_NOTIFICATIONS") is not null)
                {
                    System.Windows.Diagnostics.VisualDiagnostics.EnableVisualTreeChanged();
                }

                System.Windows.Diagnostics.VisualDiagnostics.VisualTreeChanged += onTree;
                PresentationTraceSources.Refresh();
                PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning;
                PresentationTraceSources.DataBindingSource.Listeners.Add(listener);
                Trace.Listeners.Add(listener);
                window.Dispatcher.Hooks.OperationStarted += onStart;
                window.Dispatcher.Hooks.OperationCompleted += onDone;

                var state = string.Empty;
                var since = 0L;
                var spans = new List<string>();
                while (watch.ElapsedMilliseconds < int.Parse(value))
                {
                    DoEvents();
                    var now = !(bool)visible.GetValue(layer)! ? "спрятана" : (bool)blink.GetValue(layer)! ? "видна" : "погашена";
                    if (now != state)
                    {
                        if (state.Length > 0)
                        {
                            spans.Add($"{state} {watch.ElapsedMilliseconds - since}");
                        }

                        (state, since) = (now, watch.ElapsedMilliseconds);
                    }

                    Thread.Sleep(5);
                }

                spans.Add($"{state} {watch.ElapsedMilliseconds - since}…");
                timer.Tick -= onTick;
                editor.TextEditor.TextArea.TextView.VisualLinesChanged -= onLines;
                window.LayoutUpdated -= onLayout;
                AppDomain.CurrentDomain.FirstChanceException -= onThrown;
                System.Windows.Diagnostics.VisualDiagnostics.VisualTreeChanged -= onTree;
                PresentationTraceSources.DataBindingSource.Listeners.Remove(listener);
                Trace.Listeners.Remove(listener);
                window.Dispatcher.Hooks.OperationStarted -= onStart;
                window.Dispatcher.Hooks.OperationCompleted -= onDone;
                var gaps = ticks.Zip(ticks.Skip(1), (a, b) => b - a).ToList();
                Console.WriteLine("каретка: " + string.Join(", ", spans));
                Console.WriteLine($"  тиков {ticks.Count}, промежутки {(gaps.Count == 0 ? "—" : $"{gaps.Min()}–{gaps.Max()}")} мс; пересчёты строк на {string.Join(",", lines)}; раскладок {layouts}");
                if (slow.Count > 0)
                {
                    Console.WriteLine("  долгие операции: " + string.Join("; ", slow));
                }

                if (thrown.Count > 0)
                {
                    Console.WriteLine("  исключения: " + string.Join("; ", thrown.Select(entry => $"{entry.Value} × {entry.Key}")));
                }

                if (changes.Count > 0)
                {
                    Console.WriteLine("  дерево: " + string.Join("; ", changes.OrderByDescending(entry => entry.Value).Select(entry => $"{entry.Value} × {entry.Key}")));
                }

                if (traced.Count > 0)
                {
                    Console.WriteLine("  вывод: " + string.Join("; ", traced.OrderByDescending(entry => entry.Value).Select(entry => $"{entry.Value} × {entry.Key}")));
                }

                break;
            }

            // Ошибки привязок: trace начинает их собирать, traced печатает собранное
            case "trace":
                PresentationTraceSources.Refresh();
                PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning;
                PresentationTraceSources.DataBindingSource.Listeners.Add(bindingErrors);
                break;
            case "traced":
                Console.WriteLine(errors.Count == 0
                    ? "ошибок привязок нет"
                    : "ошибки привязок:" + string.Concat(errors.Select(entry => $"{Environment.NewLine}  {entry.Value} × {entry.Key}")));
                break;

            // Что происходит в простое: пересчёты строк AvalonEdit, проходы раскладки и прокрутки окна
            case "idle":
            {
                var editor = Editor(window);
                var view = editor.TextEditor.TextArea.TextView;
                var (lines, layouts, scrolls) = (0, 0, 0);
                EventHandler onLines = (_, _) => lines++;
                EventHandler onLayout = (_, _) => layouts++;
                EventHandler onScroll = (_, _) => scrolls++;
                view.VisualLinesChanged += onLines;
                view.ScrollOffsetChanged += onScroll;
                editor.LayoutUpdated += onLayout;
                Pump(int.Parse(value));
                view.VisualLinesChanged -= onLines;
                view.ScrollOffsetChanged -= onScroll;
                editor.LayoutUpdated -= onLayout;
                Console.WriteLine($"idle {value} мс: пересчётов строк {lines}, прокруток AvalonEdit {scrolls}, проходов раскладки {layouts}");
                break;
            }
            case "cyrillic":
            {
                var found = Descendants<TextBlock>(window)
                    .Where(text => text.IsVisible && !InsideViewer(text) && text.Text.Any(IsCyrillic))
                    .Select(text => text.Text)
                    .Distinct()
                    .ToList();
                Console.WriteLine(found.Count == 0
                    ? "кириллицы вне вьюеров нет"
                    : $"кириллица вне вьюеров: {string.Join(" | ", found)}");

                // Со значением all — и весь текст видимых вьюеров
                if (value == "all")
                {
                    foreach (var viewer in Viewers(window))
                    {
                        var words = viewer.TextMap.Text
                            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                            .Where(word => word.Any(IsCyrillic))
                            .Distinct()
                            .ToList();
                        if (words.Count > 0)
                        {
                            Console.WriteLine($"кириллица во вьюере {viewer.Name}: {string.Join(" ", words.Take(12))}");
                        }
                    }
                }

                break;
            }

            // Прокрутка
            case "scroll":
                foreach (var scroll in Scrolls(window))
                {
                    scroll.ScrollToVerticalOffset(Number(value));
                }

                break;
            case "up":
                foreach (var scroll in Scrolls(window))
                {
                    scroll.ScrollToVerticalOffset(scroll.VerticalOffset - Number(value));
                }

                break;
            case "end":
                foreach (var scroll in Scrolls(window))
                {
                    scroll.ScrollToEnd();
                }

                break;
            case "goto":
                foreach (var viewer in Viewers(window))
                {
                    var at = viewer.TextMap.Text.IndexOf(value, StringComparison.Ordinal);
                    if (at >= 0)
                    {
                        viewer.ScrollToRange(new MdTextRange(at, value.Length));
                    }
                }

                break;

            // Первая сверху строка пикселей с тёмным текстом в области снимка окна x0;x1;y0;y1 и прокрутка — где на
            // экране встал текст; серая разметка порога не проходит
            case "ink":
            {
                var area = value.Split(';').Select(part => (int)Number(part)).ToArray();
                var bitmap = Render(Root(window));
                var stride = bitmap.PixelWidth * 4;
                var pixels = new byte[stride * bitmap.PixelHeight];
                bitmap.CopyPixels(pixels, stride, 0);
                var found = Enumerable.Range(area[2], area[3] - area[2])
                    .Cast<int?>()
                    .FirstOrDefault(y => Enumerable.Range(area[0], area[1] - area[0])
                        .Any(x => (0.299 * pixels[(y!.Value * stride) + (x * 4) + 2]) + (0.587 * pixels[(y.Value * stride) + (x * 4) + 1]) + (0.114 * pixels[(y.Value * stride) + (x * 4)]) < 100));
                Console.WriteLine($"ink: тёмный текст с {found?.ToString() ?? "—"}, прокрутка {Scrolls(window).FirstOrDefault()?.VerticalOffset:F1}");
                break;
            }

            // Высота редактора: разница до и после того, как окно встало на блок, — разница окна и нарисованного блока
            case "height":
                Console.WriteLine($"height: {Editor(window).ActualHeight:F1}");
                break;

            // Экранное положение строк документа с единицы N;M;… в прокрутке редактора — сдвинулось ли полотно
            case "tops":
            {
                var editor = Editor(window);
                var method = typeof(MarkdownEditor).GetMethod("LineViewportTop", BindingFlags.Instance | BindingFlags.NonPublic)!;
                Console.WriteLine("tops: " + string.Join(", ", value.Split(';').Select(line => $"{line}={method.Invoke(editor, [int.Parse(line)]) as double?:F1}")));
                break;
            }

            // Хэш пикселей полосы снимка y0;y1 во всю ширину — совпала ли картинка
            case "band":
            {
                var area = value.Split(';').Select(part => (int)Number(part)).ToArray();
                var bitmap = Render(Root(window));
                var stride = bitmap.PixelWidth * 4;
                var pixels = new byte[stride * (area[1] - area[0])];
                bitmap.CopyPixels(new Int32Rect(0, area[0], bitmap.PixelWidth, area[1] - area[0]), pixels, stride, 0);
                Console.WriteLine($"band {value}: {Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(pixels))[..16]}");
                break;
            }

            // Пробное окно наследника вместо блоков на строках первая-последняя
            case "probewindow":
            {
                var lines = value.Split('-').Select(int.Parse).ToArray();
                probe = new Border { Height = (lines[1] - lines[0] + 1) * ProbeRows.RowHeight, Background = Brushes.Khaki };
                typeof(MarkdownViewer)
                    .GetMethod("SetSourceWindow", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(Viewers(window).First(), [lines[0], lines[1], probe, new ProbeRows(lines[0], lines[1])]);
                break;
            }

            // Нажатие флажка задачи с номером — как щелчок, через автоматизацию; печатает строку задачи в Markdown
            case "task":
            {
                var viewer = Viewers(window).First();
                var mark = Descendants<MdTaskMark>(viewer).ElementAt(int.Parse(value));
                var toggle = (IToggleProvider)UIElementAutomationPeer.CreatePeerForElement(mark).GetPattern(PatternInterface.Toggle);
                try
                {
                    toggle.Toggle();
                }
                catch (ElementNotEnabledException)
                {
                    Console.WriteLine("флажок не нажимается");
                }

                Settle(window);
                Console.WriteLine($"Markdown: {TaskLines(viewer)}");
                break;
            }

            // Разворачивание или сворачивание сворачиваемого блока с номером — через автоматизацию; печатает строки свёрнутости в Markdown
            case "fold":
            {
                var viewer = Viewers(window).First();
                var folding = Descendants<FrameworkElement>(viewer)
                    .Where(element => element is MdDetailsPresenter or MdCalloutPresenter)
                    .Select(element => UIElementAutomationPeer.CreatePeerForElement(element).GetPattern(PatternInterface.ExpandCollapse))
                    .OfType<IExpandCollapseProvider>()
                    .ElementAt(int.Parse(value));
                if (folding.ExpandCollapseState == ExpandCollapseState.Expanded)
                {
                    folding.Collapse();
                }
                else
                {
                    folding.Expand();
                }

                Settle(window);
                Console.WriteLine($"Markdown: {FoldLines(viewer)}");
                break;
            }

            // Режим редактора: View, Controls, Blocks, Full
            case "mode":
                ((EditorViewModel)model.Selected).Mode = Enum.Parse<MdEditMode>(value);
                break;

            // Курсор редактора в начало строки исходника с единицы
            case "caret":
            {
                var editor = Editor(window);
                editor.SourceCaretOffset = editor.TextEditor.Document.GetLineByNumber(int.Parse(value)).Offset;
                break;
            }

            // Клавиша в AvalonEdit редактора — командой, без фокуса: Down, Up, ShiftDown, ShiftUp, PageDown, PageUp, CtrlA, Backspace
            case "key":
            {
                var area = Editor(window).TextEditor.TextArea;
                RoutedCommand command = value switch
                {
                    "Down" => EditingCommands.MoveDownByLine,
                    "Up" => EditingCommands.MoveUpByLine,
                    "ShiftDown" => EditingCommands.SelectDownByLine,
                    "ShiftUp" => EditingCommands.SelectUpByLine,
                    "PageDown" => EditingCommands.MoveDownByPage,
                    "PageUp" => EditingCommands.MoveUpByPage,
                    "CtrlA" => ApplicationCommands.SelectAll,
                    "CtrlB" => EditingCommands.ToggleBold,
                    "CtrlI" => EditingCommands.ToggleItalic,
                    "End" => EditingCommands.MoveToLineEnd,
                    "Tab" => EditingCommands.TabForward,
                    _ => EditingCommands.Backspace
                };
                command.Execute(null, area);
                break;
            }

            // Набор в AvalonEdit редактора; \n — перевод строки
            case "type":
                Editor(window).TextEditor.TextArea.PerformTextInput(value.Replace("\\n", "\n"));
                break;

            // Протяжка выделения на столько пикселей ниже верха окна правки
            case "drag":
            {
                var editor = Editor(window);
                var element = (FrameworkElement)Internal(editor, "WindowElement")!;
                var point = element.TranslatePoint(new Point(10, Number(value)), editor);
                typeof(MarkdownEditor).GetMethod("ExtendSelectionTo", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, [point]);
                break;
            }

            // Протяжка выделения мышью, которая стоит над строкой документа с единицы: точка берётся один раз, и
            // выделение тянется к ней трижды, как от движений мыши на месте; печатает строку конца выделения
            case "dragline":
            {
                var editor = Editor(window);
                DependencyObject? node = editor;
                while (node is not null and not ScrollViewer)
                {
                    node = VisualTreeHelper.GetParent(node);
                }

                var hidden = BindingFlags.Instance | BindingFlags.NonPublic;
                var top = (double)typeof(MarkdownEditor).GetMethod("LineViewportTop", hidden)!.Invoke(editor, [int.Parse(value)])!;
                var point = ((ScrollViewer)node!).TranslatePoint(new Point(40, top + 4), editor);
                var extend = typeof(MarkdownEditor).GetMethod("ExtendSelectionTo", hidden)!;
                var ends = new List<string>();
                for (var i = 0; i < 3; i++)
                {
                    var under = editor.GetSourceLineAt(point.Y);
                    extend.Invoke(editor, [point]);
                    Settle(window);
                    ends.Add($"{editor.TextEditor.TextArea.Caret.Line} (под точкой {under:F2}, окно {Internal(editor, "WindowLines")})");
                }

                Console.WriteLine($"dragline {value}: конец выделения на строках {string.Join(", ", ends)}");
                break;
            }

            // Щелчок и двойной щелчок по точке редактора x;y — как по нарисованному, без мыши и фокуса
            case "click":
            case "dblclick":
            {
                var editor = Editor(window);
                var parts = value.Split(';');
                var point = new Point(Number(parts[0]), Number(parts[1]));
                var placed = (bool)typeof(MarkdownEditor).GetMethod("PlaceCaretAt", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, [point])!;
                Settle(window);
                if (name == "dblclick" && placed)
                {
                    typeof(MarkdownEditor).GetMethod("SelectClickedWord", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, []);
                }

                Console.WriteLine($"{name}: {(placed ? $"курсор {editor.SourceCaretOffset}" : "мимо")}, выделено «{editor.TextEditor.SelectedText}»");
                break;
            }

            // Ctrl+Z и Ctrl+Y редактору — командами отмены и возврата
            case "undo":
            case "redo":
            {
                var viewer = Viewers(window).First();
                var command = name == "undo" ? ApplicationCommands.Undo : ApplicationCommands.Redo;
                Console.WriteLine($"{name}: {(command.CanExecute(null, viewer) ? "доступна" : "недоступна")}");
                command.Execute(null, viewer);
                Settle(window);
                Console.WriteLine($"Markdown: {TaskLines(viewer)}");
                break;
            }

            // Прокрутка шагами по 100 пикселей: где пробное окно на экране и какая строка вверху
            case "probescroll":
            {
                var scroll = Scrolls(window).First();
                var viewer = Viewers(window).First();
                for (var i = 0; i < int.Parse(value); i++)
                {
                    scroll.ScrollToVerticalOffset(scroll.VerticalOffset + 100);
                    Settle(window);
                    var top = probe is not null && probe.IsDescendantOf(scroll) ? probe.TranslatePoint(default, scroll).Y : double.NaN;
                    Console.WriteLine($"прокрутка {scroll.VerticalOffset:0}: окно {top:0}, вверху строка {viewer.GetVisibleSourceLine(0):0.##}");
                }

                break;
            }

            // Выделение в исходнике редактора: строки документа с единицы первая-последняя целиком
            case "sourceselect":
            {
                var editor = Editor(window);
                var lines = value.Split('-').Select(int.Parse).ToArray();
                var document = editor.TextEditor.Document;
                var start = document.GetLineByNumber(lines[0]).Offset;
                editor.SelectSource(start, document.GetLineByNumber(lines[1]).EndOffset - start);
                break;
            }

            // Выделение и разметка
            case "select":
            {
                var bounds = value.Split('-').Select(int.Parse).ToArray();
                foreach (var viewer in Viewers(window))
                {
                    viewer.Select(bounds[0], bounds[1] - bounds[0]);
                }

                break;
            }
            case "find":
                foreach (var viewer in Viewers(window))
                {
                    var at = viewer.TextMap.Text.IndexOf(value, StringComparison.Ordinal);
                    if (at >= 0)
                    {
                        viewer.Select(at, value.Length);
                    }
                }

                break;
            case "alltext":
                foreach (var viewer in Viewers(window))
                {
                    viewer.SelectAll();
                    Console.WriteLine($"текст: {viewer.SelectedText.Replace("\n", " | ")}");
                }

                break;
            case "allmarkdown":
                foreach (var viewer in Viewers(window))
                {
                    viewer.SelectAll();
                    File.WriteAllText(Path.Combine(output, "selected.md"), viewer.SelectedMarkdown);
                    var same = viewer.SelectedMarkdown == viewer.Markdown?.Replace("\r\n", "\n").TrimEnd('\n');
                    Console.WriteLine($"разметка выделенного совпадает с исходником: {same}");
                }

                break;

            // Отдельные витрины
            case "search":
                Descendants<TextBox>(window).Single(box => box.Name == "SearchBox").Text = value;
                break;
            case "replaceall":
            {
                Descendants<TextBox>(window).Single(box => box.Name == "ReplaceBox").Text = value;
                var button = Descendants<Button>(window).Single(candidate => candidate.Name == "ReplaceAllButton");
                ((IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)!).Invoke();
                break;
            }
            case "collapse":
            {
                var toggle = Descendants<ToggleButton>(window)
                    .FirstOrDefault(candidate => candidate.IsVisible && candidate.Name == "Toggle");
                if (toggle is not null)
                {
                    toggle.IsChecked = false;
                }

                break;
            }
            case "choice":
            {
                var copying = (CopyingViewModel)model.Selected;
                copying.Choice = copying.Choices[int.Parse(value)];
                Console.WriteLine($"выбрано: {Localizer.Text(copying.Choice.TitleKey)}");
                break;
            }
            case "activate":
            {
                var viewer = Viewers(window).First();
                MarkdownLink.Activate(viewer, value);
                Settle(window);
                Console.WriteLine($"ссылка {value}: сверху строка {viewer.GetVisibleSourceLine(0):0.##}");
                break;
            }
            case "margin":
                foreach (var line in value.Split('+').Select(int.Parse))
                {
                    var button = Descendants<ToggleButton>(window)
                        .FirstOrDefault(candidate => candidate.IsVisible && candidate.Tag is int tag && tag == line);
                    button?.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    Console.WriteLine($"кнопка у строки {line}: {(button is null ? "нет" : "нажата")}");
                    Settle(window);
                }

                break;
            case "editor":
            {
                var editor = Descendants<TextBox>(window).First(box => box.IsVisible && box.Name == "Editor");
                var lines = new MdTextBoxLines(editor);
                editor.RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, -120)
                {
                    RoutedEvent = UIElement.PreviewMouseWheelEvent,
                    Source = lines.Scroller
                });
                lines.Scroller!.ScrollToVerticalOffset(Number(value));
                Settle(window);
                var editorLine = lines.GetVisibleSourceLine(SyncAnchor);
                var viewerLine = Viewers(window).First().GetVisibleSourceLine(SyncAnchor);
                Console.WriteLine($"строка у точки привязки: редактор {editorLine:0.##}, вьюер {viewerLine:0.##}");
                break;
            }
            case "left":
            {
                var left = Viewers(window).Single(viewer => viewer.Name == "SyncLeft");
                ((ScrollViewer)left.Parent).ScrollToVerticalOffset(Number(value));
                break;
            }
            case "aligned":
                ((TwoPanesViewModel)model.Selected).IsAligned = true;
                break;
            case "dialect":
            {
                var sets = (SetsViewModel)model.Selected;
                sets.Selected = sets.Dialects[int.Parse(value)];
                break;
            }
            case "seton":
                ((SetsViewModel)model.Selected).IsSetOn = true;
                break;
            case "setoff":
                ((SetsViewModel)model.Selected).IsSetOn = false;
                break;

            // Свой вид
            case "look":
            {
                var look = (CustomLookViewModel)model.Selected;
                look.IsCustomFont = value[0] == '1';
                look.IsCustomHeadings = value[1] == '1';
                break;
            }
            case "keyreset":
                ((CustomLookViewModel)model.Selected).Keys.Reset();
                break;
            case "keyget":
            {
                var look = (CustomLookViewModel)model.Selected;
                var item = Descendants<PropertyItem>(window).FirstOrDefault(candidate => candidate.DisplayName == value);
                var shown = item is null
                    ? "-"
                    : string.Join(" ", Descendants<TextBlock>(item).Select(text => text.Text));
                Console.WriteLine($"ключ {value} = {look.Keys.Get(value)}; в сетке: {shown}");
                break;
            }
            case "gridsearch":
            {
                var search = Descendants<TextBox>(window).First(box => box.IsVisible && box.Name == "PART_SearchBar");
                search.Text = value;
                Settle(window);
                var shown = Descendants<PropertyItem>(window)
                    .Where(item => item.IsVisible)
                    .Select(item => item.DisplayName)
                    .ToList();
                Console.WriteLine($"поиск '{value}': показано {shown.Count}: {string.Join(", ", shown.Take(6))}");
                break;
            }
            case "grid":
            {
                var key = value[..value.IndexOf('=')];
                var text = value[(value.IndexOf('=') + 1)..];
                var item = Descendants<PropertyItem>(window).FirstOrDefault(candidate => candidate.DisplayName == key);
                Console.WriteLine($"сетка {key}: редактор {(item is null ? "не найден" : item.EditorElement?.GetType().Name)}");
                switch (item?.EditorElement)
                {
                    case NumericUpDown numeric:
                        numeric.Value = Number(text);
                        break;
                    case TextBox box:
                        box.Text = text;
                        box.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
                        break;
                    case Grid host:
                        var popup = host.Children.OfType<Popup>().First();
                        var picker = LogicalTreeHelper.GetChildren(popup.Child).OfType<ColorPicker>().First();
                        picker.SelectedBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(text));
                        break;
                }

                break;
            }
            case "pickershot":
            {
                var item = Descendants<PropertyItem>(window).First(candidate => candidate.DisplayName == value);
                var popup = ((Grid)item.EditorElement).Children.OfType<Popup>().First();

                // BYDESIGN: всплывающее окно не открывается — WPF вывел бы его на экран; его содержимое рисуется
                // в картинку на пурпурном фоне, чтобы было видно прозрачное.
                var content = (FrameworkElement)popup.Child;
                content.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                content.Arrange(new Rect(content.DesiredSize));
                content.UpdateLayout();
                Write(Render(content, 1, Brushes.Magenta), Path.Combine(output, $"picker-{shot:00}.png"));
                break;
            }

            // Часть окна крупно: x;y;ширина;высота;масштаб
            case "zoom":
            {
                var parts = value.Split(';').Select(Number).ToArray();
                var scale = parts[4];
                var crop = new Int32Rect(
                    (int)(parts[0] * scale),
                    (int)(parts[1] * scale),
                    (int)(parts[2] * scale),
                    (int)(parts[3] * scale));
                Write(new CroppedBitmap(Render(Root(window), scale), crop), Path.Combine(output, $"zoom-{shot:00}.png"));
                break;
            }
        }

        Settle(window);
        if (name is "mode" or "caret" or "key" or "type" or "drag" or "undo" or "redo")
        {
            Console.WriteLine($"{step}: {WindowReport(Editor(window))}");
        }

        var title = string.Concat(step.Take(40).Select(letter => char.IsLetterOrDigit(letter) ? letter : '_'));
        Write(Render(Root(window)), Path.Combine(output, $"{shot++:00}-{title}.png"));
    }

    window.Close();
    app.Shutdown();
});
thread.SetApartmentState(ApartmentState.STA);
thread.Start();
thread.Join();

static IEnumerable<T> Descendants<T>(DependencyObject root)
    where T : DependencyObject
{
    for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
    {
        var child = VisualTreeHelper.GetChild(root, i);
        if (child is T found)
        {
            yield return found;
        }

        foreach (var deeper in Descendants<T>(child))
        {
            yield return deeper;
        }
    }
}

static List<MarkdownViewer> Viewers(Window window)
    => Descendants<MarkdownViewer>(window)
        .Where(viewer => viewer.IsVisible)
        .ToList();

static List<ScrollViewer> Scrolls(Window window)
    => Descendants<ScrollViewer>(window)
        .Where(scroll => scroll.IsVisible && Descendants<MarkdownViewer>(scroll).Any())
        .ToList();

static bool InsideViewer(DependencyObject element)
{
    for (var node = VisualTreeHelper.GetParent(element); node is not null; node = VisualTreeHelper.GetParent(node))
    {
        if (node is MarkdownViewer)
        {
            return true;
        }
    }

    return false;
}

static bool IsCyrillic(char letter) => letter is >= 'А' and <= 'я' or 'Ё' or 'ё';

static string TaskLines(MarkdownViewer viewer)
    => string.Join(" | ", (viewer.Markdown ?? string.Empty)
        .Split('\n')
        .Where(line => line.TrimStart().StartsWith("- [", StringComparison.Ordinal)));

static MarkdownEditor Editor(Window window) => Descendants<MarkdownEditor>(window).First(editor => editor.IsVisible);

static object? Internal(MarkdownEditor editor, string name)
    => typeof(MarkdownEditor).GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor);

// Строки окна, сколько строк разложено, чужие строки в видимой части AvalonEdit, виден ли курсор
static string WindowReport(MarkdownEditor editor)
{
    var foreign = (IReadOnlyList<int>)typeof(MarkdownEditor).GetMethod("ForeignLines", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, [])!;
    var visible = (bool)typeof(MarkdownEditor).GetMethod("IsCaretVisible", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, [])!;
    var lines = Internal(editor, "WindowLines") is ValueTuple<int, int> window ? $"{window.Item1}–{window.Item2}" : "нет";
    return $"окно {lines} {WindowPlace(editor.Items.Cast<BlockVm>()) ?? "нигде"}, разложено {Internal(editor, "LaidOutLines")}, " +
        $"чужие строки: {(foreign.Count == 0 ? "нет" : string.Join(",", foreign))}, " +
        $"курсор в строке {editor.TextEditor.TextArea.Caret.Line}, виден: {(visible ? "да" : "нет")}";
}

// Где стоит окно: наверху или внутри контейнеров — цитаты, плашки, сворачиваемого блока, пункта
static string? WindowPlace(IEnumerable<NodeVm> nodes)
{
    foreach (var node in nodes)
    {
        if (node.GetType().Name == "SourceWindowVm")
        {
            return "наверху";
        }

        IEnumerable<NodeVm> inside = node switch
        {
            QuoteVm quote => quote.Blocks,
            CalloutVm callout => callout.Blocks,
            DetailsVm details => details.Blocks,
            ListVm list => list.Items,
            ListItemVm item => item.Blocks,
            _ => []
        };
        if (WindowPlace(inside) is { } place)
        {
            var here = node.GetType().Name[..^2];
            return place == "наверху" ? $"в {here}" : $"в {here} › {place[2..]}";
        }
    }

    return null;
}

static string FoldLines(MarkdownViewer viewer)
    => string.Join(" | ", (viewer.Markdown ?? string.Empty)
        .Split('\n')
        .Where(line => line.Contains("<details", StringComparison.Ordinal) || line.Contains("[!", StringComparison.Ordinal)));

static FrameworkElement Root(Window window) => (FrameworkElement)VisualTreeHelper.GetChild(window, 0);

static double Number(string text) => double.Parse(text, CultureInfo.InvariantCulture);

static void Settle(Window window)
{
    for (var i = 0; i < 5; i++)
    {
        window.UpdateLayout();
        DoEvents();
    }
}

static void Pump(int milliseconds)
{
    var watch = Stopwatch.StartNew();
    while (watch.ElapsedMilliseconds < milliseconds)
    {
        DoEvents();
        Thread.Sleep(20);
    }
}

static void DoEvents()
{
    var frame = new DispatcherFrame();
    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, () => frame.Continue = false);
    Dispatcher.PushFrame(frame);
}

static RenderTargetBitmap Render(FrameworkElement element, double scale = 1, Brush? backdrop = null)
{
    var bitmap = new RenderTargetBitmap(
        (int)(element.ActualWidth * scale),
        (int)(element.ActualHeight * scale),
        96 * scale,
        96 * scale,
        PixelFormats.Pbgra32);
    if (backdrop is not null)
    {
        var fill = new DrawingVisual();
        using (var context = fill.RenderOpen())
        {
            context.DrawRectangle(backdrop, null, new Rect(0, 0, element.ActualWidth, element.ActualHeight));
        }

        bitmap.Render(fill);
    }

    bitmap.Render(element);
    return bitmap;
}

static void Write(BitmapSource bitmap, string path)
{
    var encoder = new PngBitmapEncoder();
    encoder.Frames.Add(BitmapFrame.Create(bitmap));
    using var file = File.Create(path);
    encoder.Save(file);
    Console.WriteLine($"снимок: {path}");
}

[DllImport("user32.dll")]
static extern int GetWindowLong(IntPtr window, int index);

[DllImport("user32.dll")]
static extern int SetWindowLong(IntPtr window, int index, int value);

/// <summary>Строки пробного окна одной высоты, все видны.</summary>
internal sealed class ProbeRows(int first, int last) : IMdSourceRows
{
    public event EventHandler? RowsChanged
    {
        add { }
        remove { }
    }

    public const double RowHeight = 20;

    public double? GetSourceLineOffset(double line) => (Math.Clamp(line, first, last + 1) - first) * RowHeight;

    public double? GetSourceLineAt(double offset) => first + Math.Clamp(offset / RowHeight, 0, last + 1 - first);

    public IReadOnlyList<(int Line, double Top, double Height)> VisibleRows()
        => Enumerable.Range(first, last - first + 1)
            .Select(line => (line, (line - first) * RowHeight, RowHeight))
            .ToList();
}

/// <summary>Слушатель отладочного вывода и трассировки WPF: считает строки, обрезанные до начала.</summary>
sealed class CountingListener(Dictionary<string, int> counts) : TraceListener
{
    private readonly StringBuilder _line = new();

    public override void Write(string? message) => _line.Append(message);

    public override void WriteLine(string? message)
    {
        _line.Append(message);
        var text = System.Text.RegularExpressions.Regex.Replace(_line.ToString(), @"HashCode=-?\d+", "HashCode");
        var key = text.Length > 700 ? text[..700] : text;
        lock (counts)
        {
            counts[key] = counts.GetValueOrDefault(key) + 1;
        }

        _line.Clear();
    }
}
