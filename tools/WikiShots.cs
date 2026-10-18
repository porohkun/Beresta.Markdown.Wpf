#:project ../Markdig.Wpf.Viewer.Showcase/Markdig.Wpf.Viewer.Showcase.csproj
#:property TargetFramework=net10.0-windows
#:property UseWPF=true
#:property PublishAot=false
#:property PublishTrimmed=false

using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MarkdigWpfViewer;
using MarkdigWpfViewer.Extensions;
using MarkdigWpfViewer.Kit;
using MarkdigWpfViewer.Scrolling;
using MarkdigWpfViewer.Search;
using MarkdigWpfViewer.Selection;
using MarkdigWpfViewer.Showcase;
using MarkdigWpfViewer.Showcase.Showcases.TwoPanes;
using Polygon = System.Windows.Shapes.Polygon;

const int ExtendedStyle = -20;
const int NoActivate = 0x08000000;
const int ToolWindow = 0x00000080;

const string Overview = """
    # Markdig.Wpf.Viewer

    ![Banner](banner.png)

    A **WPF control** that renders *Markdown*: ~~HTML~~ native WPF elements, `virtualized` and selectable.
    Links like [the repository](https://github.com/porohkun/Markdig.Wpf.Viewer) open in the browser.

    ## Lists and tasks

    - first item
    - second item
      - nested item
    - [x] done task
    - [ ] open task

    ## Table

    | Feature | Status | Since |
    |:--------|:------:|------:|
    | Selection | ready | 0.3 |
    | Footnotes | ready | 0.5 |

    > Quotes keep their own blocks, and footnotes stay where they are written[^1].

    [^1]: Like this one.

    ```csharp
    var viewer = new MarkdownViewer { Markdown = text };
    ```

    Press <kbd>Ctrl</kbd>+<kbd>C</kbd> to copy, H<sub>2</sub>O and E = mc<sup>2</sup> work too.
    """;

const string LeftText = "# Release notes\n\nThe viewer renders markdown.\n\nIt supports tables.\n\nCopying puts plain text.";
const string RightText = "# Release notes\n\nThe viewer renders markdown.\n\nIt supports tables and footnotes.\n\n"
    + "Two panes stand side by side\nwith empty places opposite the changes.\n\nCopying puts plain text.";

var names = new List<string>();
var output = Path.GetFullPath(Path.Combine(ScriptDirectory(), "..", "docs", "wiki", "images"));
for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--out")
    {
        output = args[++i];
    }
    else
    {
        names.AddRange(args[i].Split(',', StringSplitOptions.RemoveEmptyEntries));
    }
}

Directory.CreateDirectory(output);
var taken = new HashSet<string>();

var thread = new Thread(() =>
{
    var app = new App();
    app.InitializeComponent();
    app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
    var loader = new ShotLoader();
    var resolver = new ShotResolver();

    // Подписи интерфейса — английские, как в теме библиотеки
    var generic = new ResourceDictionary
    {
        Source = new Uri("pack://application:,,,/Markdig.Wpf.Viewer;component/Themes/Generic.xaml")
    };
    var captions = generic.Keys
        .OfType<string>()
        .Where(key => key.StartsWith("Md.", StringComparison.Ordinal) && generic[key] is string)
        .ToDictionary(key => key, key => (string)generic[key]);

    // Вьюер
    if (Wanted("overview.png"))
    {
        Shot("overview.png", new MarkdownViewer { Markdown = Overview, ImageLoader = loader }, 760);
    }

    if (Wanted("selection.png"))
    {
        var viewer = new MarkdownViewer
        {
            Markdown = "Text is selected **across blocks** with the mouse,\nand search results are highlighted with decorations."
                + "\n\nSelection keeps working in lists:\n\n- found in the first item\n- and in the second item"
        };
        Shot("selection.png", viewer, 640, () =>
        {
            var text = viewer.TextMap.Text;
            var start = text.IndexOf("across", StringComparison.Ordinal);
            viewer.Select(start, text.IndexOf("decorations", StringComparison.Ordinal) - start);
            var found = new List<MdDecoration>();
            for (var index = text.IndexOf("item", StringComparison.Ordinal);
                 index >= 0;
                 index = text.IndexOf("item", index + 4, StringComparison.Ordinal))
            {
                found.Add(new(new(index, 4), new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xC1, 0x07))));
            }

            viewer.Decorations = found;
        });
    }

    if (Wanted("line-numbers.png"))
    {
        Shot("line-numbers.png", new MarkdownViewer
        {
            Markdown = "# Line numbers\n\nNumbers follow the source file,\nblank lines keep theirs too.\n\n"
                + "```csharp\nint Add(int a, int b)\n{\n    return a + b;\n}\n```\n\n| a | b |\n|---|---|\n| 1 | 2 |",
            ShowSourceLineNumbers = true,
            ShowCodeLineNumbers = true
        }, 640);
    }

    if (Wanted("code-block.png"))
    {
        var viewer = new MarkdownViewer
        {
            Markdown = "```csharp\npublic sealed record Note(string Title, string Text);\n\nvar note = new Note(\"Hello\", \"World\");\n```"
        };
        Shot("code-block.png", viewer, 640, () =>
        {
            foreach (var copy in Descendants<Button>(viewer).Where(button => button.Name == "PART_Copy"))
            {
                copy.Visibility = Visibility.Visible;
            }
        });
    }

    if (Wanted("widgets.png"))
    {
        var viewer = (MarkdownViewer)XamlReader.Parse("""
            <mdv:MarkdownViewer xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                                xmlns:mdv="clr-namespace:MarkdigWpfViewer;assembly=Markdig.Wpf.Viewer"
                                xmlns:p="clr-namespace:MarkdigWpfViewer.Presenters;assembly=Markdig.Wpf.Viewer"
                                xmlns:ext="clr-namespace:MarkdigWpfViewer.Showcase.Showcases.Extensions;assembly=Markdig.Wpf.Viewer.Showcase">
                <mdv:MarkdownViewer.Extensions>
                    <ext:NoteExtension>
                        <ext:NoteExtension.Template>
                            <DataTemplate>
                                <Border Margin="0,4,0,8" Padding="12,8" CornerRadius="4" BorderThickness="4,0,0,0"
                                        BorderBrush="{DynamicResource PrimaryBrush}" Background="{DynamicResource SecondaryRegionBrush}">
                                    <StackPanel>
                                        <TextBlock Text="{Binding Title, StringFormat='▾ {0}'}" FontWeight="SemiBold"
                                                   Foreground="{DynamicResource PrimaryBrush}" Margin="0,0,0,4" />
                                        <p:MdBlocksPresenter Blocks="{Binding Blocks}" />
                                    </StackPanel>
                                </Border>
                            </DataTemplate>
                        </ext:NoteExtension.Template>
                    </ext:NoteExtension>
                    <ext:MentionExtension>
                        <ext:MentionExtension.Template>
                            <DataTemplate>
                                <Border Padding="6,0" CornerRadius="8" Background="{DynamicResource PrimaryBrush}">
                                    <TextBlock Text="{Binding Name, StringFormat='@{0}'}" Foreground="White" />
                                </Border>
                            </DataTemplate>
                        </ext:MentionExtension.Template>
                    </ext:MentionExtension>
                </mdv:MarkdownViewer.Extensions>
            </mdv:MarkdownViewer>
            """);
        viewer.Markdown = "Ask @anna or @boris about custom syntax.\n\n:::note Your own block\nA container with **markdown inside**:"
            + "\n\n- it is selectable\n- and copied with its text\n:::";
        Shot("widgets.png", viewer, 640);
    }

    if (Wanted("custom-template.png"))
    {
        var viewer = (MarkdownViewer)XamlReader.Parse("""
            <mdv:MarkdownViewer xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                                xmlns:mdv="clr-namespace:MarkdigWpfViewer;assembly=Markdig.Wpf.Viewer"
                                xmlns:p="clr-namespace:MarkdigWpfViewer.Presenters;assembly=Markdig.Wpf.Viewer"
                                xmlns:s="clr-namespace:MarkdigWpfViewer.Styling;assembly=Markdig.Wpf.Viewer"
                                xmlns:vm="clr-namespace:MarkdigWpfViewer.ViewModels;assembly=Markdig.Wpf.Viewer">
                <mdv:MarkdownViewer.StyleProvider>
                    <s:MdStyleProvider BasedOn="{StaticResource Md.DefaultStyleProvider}">
                        <s:MdStyleProvider.HeadingTemplate>
                            <DataTemplate DataType="{x:Type vm:HeadingVm}">
                                <Border Margin="0,12,0,4" Padding="10,0,0,0" BorderThickness="4,0,0,0"
                                        BorderBrush="{DynamicResource PrimaryBrush}">
                                    <p:MdHeadingBlock Level="{Binding Level}" Runs="{Binding Inlines}"
                                                      Style="{DynamicResource Md.Heading.Style}" FontFamily="Georgia" Margin="0" />
                                </Border>
                            </DataTemplate>
                        </s:MdStyleProvider.HeadingTemplate>
                    </s:MdStyleProvider>
                </mdv:MarkdownViewer.StyleProvider>
            </mdv:MarkdownViewer>
            """);
        viewer.Markdown = "# Your own heading template\n\nOnly the heading template is replaced: a bar on the left and a serif font."
            + "\nParagraphs, lists and code keep the standard look.\n\n## Second level\n\n- the rest stays default";
        Shot("custom-template.png", viewer, 640);
    }

    // Поиск, пометки, две панели, кнопки на полях
    if (Wanted("search.png"))
    {
        var viewer = new MarkdownViewer
        {
            Markdown = "## Search\n\nSearch marks every match of the query and the current one in its own color."
                + "\nThe application owns the search field; the viewer finds the matches.\n\n"
                + "- matches are found in the whole document\n- the current match is the one the viewer scrolls to"
        };
        Shot("search.png", viewer, 640, () => new MdSearch(viewer) { Query = "match" }.Next());
    }

    if (Wanted("marks.png"))
    {
        var viewer = new MarkdownViewer
        {
            Markdown = "## Marks\n\nA fill paints a piece of text, a line underlines a comment,\nand a frame draws attention to a word."
                + "\nA caret stands where the other version has text.\n\nA row spans the whole width of the document."
                + "\n\nBars and icons stand in the margin next to the line numbers.",
            ShowSourceLineNumbers = true
        };
        Shot("marks.png", viewer, 640, () =>
        {
            var text = viewer.TextMap.Text;
            MdTextRange Word(string word) => new(text.IndexOf(word, StringComparison.Ordinal), word.Length);
            viewer.MarkLayers.Add(new MdMarkLayer("Demo")
            {
                Marks =
                [
                    new MdMark(Word("fill")) { Style = "Found" },
                    new MdMark(Word("a comment"), MdMarkKind.Underline) { Style = "Comment" },
                    new MdMark(Word("a word"), MdMarkKind.Frame) { Style = "Changed" },
                    new MdMark(new MdTextRange(Word("where").Start, 0), MdMarkKind.Caret) { Style = "Removed" },
                    new MdMark(6, 6) { Style = "Added" },
                    new MdMark(2, 4, MdMarkKind.Bar) { Style = "Changed" },
                    new MdMark(6, 6, MdMarkKind.Bar) { Style = "Added" },
                    new MdMark(8, 8, MdMarkKind.Icon) { Style = "Comment", Icon = Geometry.Parse("M1,1 H13 V9 H6 L3,12 V9 H1 Z") }
                ]
            });
        });
    }

    if (Wanted("two-panes.png"))
    {
        var (left, right, grid) = Panes();
        Shot("two-panes.png", grid, 900, () => new MdPaneAligner(left, right, Diff(left, right)).Align());
    }

    if (Wanted("two-panes-connections.png"))
    {
        var (left, right, grid) = Panes();
        var connections = (Canvas)grid.Children[1];
        Shot("two-panes-connections.png", grid, 900, () =>
        {
            Diff(left, right);
            var leftLines = LeftText.Split('\n');
            var rightLines = RightText.Split('\n');
            var leftCoordinates = left.GetCoordinates(connections);
            var rightCoordinates = right.GetCoordinates(connections);
            foreach (var hunk in LineDiff.Hunks(LineDiff.Match(leftLines, rightLines), leftLines, rightLines))
            {
                if (Band(leftCoordinates, hunk.LeftStart, hunk.LeftEnd) is not { } from
                    || Band(rightCoordinates, hunk.RightStart, hunk.RightEnd) is not { } to)
                {
                    continue;
                }

                var style = hunk.LeftEnd < hunk.LeftStart ? "Added" : hunk.RightEnd < hunk.RightStart ? "Removed" : "Changed";
                connections.Children.Add(new Polygon
                {
                    Points = { new(0, from.Top), new(48, to.Top), new(48, to.Bottom), new(0, from.Bottom) },
                    Fill = (Brush)app.FindResource($"Md.Mark.{style}.Brush"),
                    Stroke = (Brush)app.FindResource($"Md.Mark.{style}.LineBrush"),
                    StrokeThickness = 1
                });
            }
        });
    }

    if (Wanted("margin-buttons.png"))
    {
        var viewer = new MarkdownViewer
        {
            Markdown = "## Bookmarks\n\nThe column on the left belongs to the application.\nIts buttons stand at the source lines in view."
                + "\n\nA click makes a line a bookmark,\nand the application marks it across the whole width.\n\n"
                + "```csharp\nvar rows = Viewer.GetCoordinates(Marks).VisibleRows();\n```"
        };
        var column = new Canvas();
        var grid = new Grid { ColumnDefinitions = { new() { Width = new(28) }, new() } };
        Grid.SetColumn(viewer, 1);
        grid.Children.Add(column);
        grid.Children.Add(viewer);
        Shot("margin-buttons.png", grid, 640, () =>
        {
            var dot = (Style)XamlReader.Parse("""
                <Style xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                       xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                       TargetType="ToggleButton">
                    <Setter Property="Width" Value="12" />
                    <Setter Property="Height" Value="12" />
                    <Setter Property="Template">
                        <Setter.Value>
                            <ControlTemplate TargetType="ToggleButton">
                                <Ellipse x:Name="Dot" Fill="Transparent" Stroke="{DynamicResource BorderBrush}" StrokeThickness="1" />
                                <ControlTemplate.Triggers>
                                    <Trigger Property="IsChecked" Value="True">
                                        <Setter TargetName="Dot" Property="Fill" Value="{DynamicResource PrimaryBrush}" />
                                        <Setter TargetName="Dot" Property="Stroke" Value="{DynamicResource PrimaryBrush}" />
                                    </Trigger>
                                </ControlTemplate.Triggers>
                            </ControlTemplate>
                        </Setter.Value>
                    </Setter>
                </Style>
                """);
            int[] bookmarks = [3, 6];
            var lines = viewer.Markdown!.Split('\n');
            foreach (var (line, top, height) in viewer.GetCoordinates(column).VisibleRows())
            {
                if (string.IsNullOrWhiteSpace(lines[line]))
                {
                    continue;
                }

                var button = new ToggleButton { Style = dot, IsChecked = bookmarks.Contains(line) };
                Canvas.SetLeft(button, 8);
                Canvas.SetTop(button, top + (height - 12) / 2);
                column.Children.Add(button);
            }

            viewer.MarkLayers.Add(new MdMarkLayer("Bookmarks")
            {
                Marks = bookmarks
                    .Select(line => new MdMark(line, line) { Style = "Comment" })
                    .ToList()
            });
        });
    }

    // Готовые расширения
    var github = new GitHubSet { Resolver = resolver };
    github.IssueTemplates["#"] = "https://github.com/org/repo/issues/{0}";
    Kit("kit-github.png", github, """
        ---
        title: Release notes
        version: 0.6.0
        ---

        Thanks @anna for #12 and @boris for #15 :tada:

        The accent color is `#0969DA`, the warning one is `rgb(154, 103, 0)`.
        """);

    var gitlab = new GitLabSet();
    gitlab.IssueTemplates["#"] = "https://gitlab.com/group/project/-/issues/{0}";
    Kit("kit-gitlab.png", gitlab, """
        [[_TOC_]]

        ## Staging

        Fixes #42 :rocket: — the new badge color is `#FC6D26`.

        ## Production

        Released after the review :white_check_mark:
        """);

    Kit("kit-obsidian.png", new ObsidianSet { Resolver = resolver }, """
        ---
        tags: [project, wpf]
        status: draft
        ---

        > [!tip] Daily note
        > Links to [[Roadmap]], [[Missing page]] and the section [[#Tasks]].

        Tagged #idea %%a hidden comment%% with an embedded image:

        ![[banner.png|240]]

        ## Tasks

        - [/] in progress
        - [>] moved
        - [!] important
        """);

    Kit("kit-mkdocs.png", new MkDocsSet(), """
        !!! note "Install"
            Run `dotnet add package Markdig.Wpf.Viewer`.

        ??? warning "Collapsed at first"
            Click the title to read.

        ???+ example "Open at first"
            A collapsible admonition that starts open.
        """);

    Kit("kit-docusaurus.png", new DocusaurusSet(), """
        :::tip Pro tip
        Containers take a kind and a title.
        :::

        :::danger[A title in brackets]
        As in Docusaurus 3.
        :::

        :::details Click to expand
        A collapsible block from VuePress.
        :::
        """);

    Kit("kit-hackmd.png", new HackMDSet(), """
        [TOC]

        ## Meeting notes

        :::success
        Shipped on Friday!
        :::

        {%youtube jNQXAC9IVRw %}

        :::spoiler Open questions
        Hidden until expanded.
        :::
        """);

    Kit("kit-chat.png", new ChatSet { Resolver = resolver }, """
        @anna the build is green :white_check_mark:

        The answer is ||42|| :wink:
        """);

    Kit("kit-callouts.png", new MdExtensionSet { Extensions = { new ObsidianCallouts(), new MkDocsAdmonitions(), new CalloutContainers() } }, """
        > [!question]- Obsidian, collapsed
        > Hidden until expanded.

        > [!success] Obsidian with a title
        > Any blocks inside.

        !!! bug "MkDocs"
            Content is indented by four spaces.

        :::info Docusaurus and HackMD
        A container with a kind and a title.
        :::
        """);

    var issues = new IssueLinks();
    issues.Templates["#"] = "https://github.com/org/repo/issues/{0}";
    var linkExtensions = new MdExtensionSet
    {
        Extensions = { new TableOfContents(), new WikiLinks { Resolver = resolver }, new Tags(), new Mentions { Resolver = resolver }, issues }
    };
    Kit("kit-links.png", linkExtensions, """
        [TOC]

        ## Links

        A wiki link to [[Roadmap]], a missing [[Missing page]], a tag #release, a mention @anna
        and issue #12.

        ## Table of contents

        Follows the headings as the text changes.
        """);

    var textExtensions = new MdExtensionSet
    {
        Extensions = { new Chips(), new ColorChips(), new CriticMarkup(), new Spoilers(), new TaskStatuses(), new FootnoteTips() }
    };
    Kit("kit-text.png", textExtensions, """
        Status: ::done::{.success} ::review::{.info} ::risk::{.warning} ::blocked::{.danger}, color `#8250DF`.

        Text {++added++}, {--removed--}, {~~old~>new~~} and {==marked==}{>>a comment<<}.

        A ||spoiler||, a footnote in a tooltip[^1].

        - [/] in progress
        - [?] in question

        [^1]: The note.
        """);

    var mediaExtensions = new MdExtensionSet
    {
        Extensions = { new PropertiesCard(), new VideoCards(), new EmojiShortcodes(), new Shortcodes(), new CsvTables() }
    };
    Kit("kit-media.png", mediaExtensions, """
        ---
        title: Media and data
        tags: [video, csv]
        ---

        [![A video card](cover.png)](https://www.youtube.com/watch?v=jNQXAC9IVRw)

        {%pdf "https://example.com/guide.pdf" %} :movie_camera:

        ```csv
        City,Population,"Area, km²"
        Moscow,13 010 112,2561
        Novosibirsk,1 633 595,502
        ```
        """);

    // BYDESIGN: тёмная тема — последней: она переключает тему всего приложения.
    if (Wanted("dark-theme.png"))
    {
        new MainWindowViewModel().Options.IsDark = true;
        Shot("dark-theme.png", new MarkdownViewer { Markdown = Overview, ImageLoader = loader }, 760);
    }

    app.Shutdown();

    void Kit(string name, MdExtension extension, string markdown)
    {
        if (!Wanted(name))
        {
            return;
        }

        var viewer = new MarkdownViewer { Markdown = markdown.ReplaceLineEndings("\n"), ImageLoader = loader };
        viewer.Extensions.Add(extension);
        Shot(name, viewer, 640);
    }

    void Shot(string name, FrameworkElement content, double width, Action? prepare = null)
    {
        var frame = new Border
        {
            Width = width,
            Padding = new(20, 16, 20, 16),
            Child = content
        };
        frame.SetResourceReference(Border.BackgroundProperty, "RegionBrush");
        frame.SetResourceReference(TextElement.ForegroundProperty, "PrimaryTextBrush");
        foreach (var (key, caption) in captions)
        {
            frame.Resources[key] = caption;
        }

        // BYDESIGN: окно за экраном и не становится активным: снимок не мелькает перед пользователем и не забирает его ввод.
        var window = new Window
        {
            Style = new Style(typeof(Window)),
            Content = frame,
            SizeToContent = SizeToContent.WidthAndHeight,
            Left = -10000,
            Top = -10000,
            ShowActivated = false,
            ShowInTaskbar = false,
            WindowStyle = WindowStyle.None
        };
        window.SourceInitialized += (_, _) =>
        {
            var handle = new WindowInteropHelper(window).Handle;
            SetWindowLong(handle, ExtendedStyle, GetWindowLong(handle, ExtendedStyle) | NoActivate | ToolWindow);
        };
        window.Show();
        Settle(window);
        prepare?.Invoke();
        Settle(window);
        Thread.Sleep(200);
        Settle(window);

        var bitmap = new RenderTargetBitmap(
            (int)Math.Ceiling(frame.ActualWidth),
            (int)Math.Ceiling(frame.ActualHeight),
            96,
            96,
            PixelFormats.Pbgra32);
        bitmap.Render((Visual)VisualTreeHelper.GetChild(window, 0));
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using (var file = File.Create(Path.Combine(output, name)))
        {
            encoder.Save(file);
        }

        Console.WriteLine($"снимок: {name} {bitmap.PixelWidth}x{bitmap.PixelHeight}");
        window.Close();
    }
});
thread.SetApartmentState(ApartmentState.STA);
thread.Start();
thread.Join();

foreach (var name in names.Except(taken))
{
    Console.WriteLine($"нет такой картинки: {name}");
}

bool Wanted(string name)
{
    if (names.Count > 0 && !names.Contains(name))
    {
        return false;
    }

    taken.Add(name);
    return true;
}

static (MarkdownViewer Left, MarkdownViewer Right, Grid Grid) Panes()
{
    var left = new MarkdownViewer { Markdown = LeftText };
    var right = new MarkdownViewer { Markdown = RightText };
    var grid = new Grid { ColumnDefinitions = { new(), new() { Width = new(48) }, new() } };
    Grid.SetColumn(right, 2);
    grid.Children.Add(left);
    var connections = new Canvas { ClipToBounds = true };
    Grid.SetColumn(connections, 1);
    grid.Children.Add(connections);
    grid.Children.Add(right);
    return (left, right, grid);
}

static MdLineMap Diff(MarkdownViewer left, MarkdownViewer right)
{
    var leftLines = LeftText.Split('\n');
    var rightLines = RightText.Split('\n');
    var matches = LineDiff.Match(leftLines, rightLines);
    left.MarkLayers.Add(new MdMarkLayer("Diff")
    {
        Marks = LineDiff.Unmatched(leftLines, matches.Select(match => match.Left))
            .Select(chunk => new MdMark(chunk.Start, chunk.End) { Style = "Removed" })
            .ToList()
    });
    right.MarkLayers.Add(new MdMarkLayer("Diff")
    {
        Marks = LineDiff.Unmatched(rightLines, matches.Select(match => match.Right))
            .Select(chunk => new MdMark(chunk.Start, chunk.End) { Style = "Added" })
            .ToList()
    });
    return new(matches.SelectMany(match => new[] { ((double)match.Left, (double)match.Right), (match.Left + 1.0, match.Right + 1.0) }));
}

static (double Top, double Bottom)? Band(MdCoordinates coordinates, int start, int end)
    => end >= start
        ? coordinates.LinesRect(start, end) is { } rect ? (rect.Top, rect.Bottom) : null
        : coordinates.LineTop(start) is { } top ? (top, top) : null;

static void Settle(Window window)
{
    for (var i = 0; i < 8; i++)
    {
        window.UpdateLayout();
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, () => frame.Continue = false);
        Dispatcher.PushFrame(frame);
    }
}

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

static string ScriptDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;

[DllImport("user32.dll")]
static extern int GetWindowLong(IntPtr window, int index);

[DllImport("user32.dll")]
static extern int SetWindowLong(IntPtr window, int index, int value);

/// <summary>Картинки снимков: баннер по адресу с <c>banner</c>, остальное — заставка.</summary>
sealed class ShotLoader : IMdImageLoader
{
    public Task<ImageSource?> LoadAsync(string url, Uri? uri, CancellationToken cancellationToken)
        => Task.FromResult<ImageSource?>(url.Contains("banner", StringComparison.Ordinal) ? Banner() : Still());

    private static ImageSource Banner()
    {
        const int width = 640;
        const int height = 160;
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            var background = new LinearGradientBrush(Color.FromRgb(0x32, 0x6C, 0xF3), Color.FromRgb(0x7A, 0x3F, 0xD9), 20);
            context.DrawRoundedRectangle(background, null, new Rect(0, 0, width, height), 16, 16);
            var mark = Text("M↓", FontWeights.Bold, 72, Brushes.White);
            context.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)), null, new Rect(32, 28, 116, 104), 12, 12);
            context.DrawText(mark, new Point(32 + (116 - mark.Width) / 2, 28 + (104 - mark.Height) / 2));
            context.DrawText(Text("Markdig.Wpf.Viewer", FontWeights.SemiBold, 36, Brushes.White), new Point(176, 46));
            var subtitle = new SolidColorBrush(Color.FromArgb(0xDD, 0xFF, 0xFF, 0xFF));
            context.DrawText(Text("Markdown for WPF", FontWeights.Normal, 22, subtitle), new Point(178, 94));
        }

        return Render(visual, width, height);
    }

    private static ImageSource Still()
    {
        const int width = 480;
        const int height = 270;
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            var background = new LinearGradientBrush(Color.FromRgb(0x0E, 0x74, 0x90), Color.FromRgb(0x1A, 0x7F, 0x37), 35);
            context.DrawRectangle(background, null, new Rect(0, 0, width, height));
            context.DrawEllipse(new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF)), null, new Point(360, 80), 120, 120);
            context.DrawEllipse(new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF)), null, new Point(90, 230), 90, 90);
        }

        return Render(visual, width, height);
    }

    private static FormattedText Text(string text, FontWeight weight, double size, Brush brush)
        => new(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, weight, FontStretches.Normal),
            size,
            brush,
            1.0);

    private static ImageSource Render(Visual visual, int width, int height)
    {
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }
}

/// <summary>Страницы и люди снимков: нет только страницы «Missing page».</summary>
sealed class ShotResolver : IMdLinkResolver
{
    public bool PageExists(string page) => page != "Missing page";

    public string? DisplayName(string user) => user switch
    {
        "anna" => "Anna Smith",
        "boris" => "Boris Ivanov",
        _ => null
    };
}
