#:project ../Beresta.Markdown.Wpf.Showcase/Beresta.Markdown.Wpf.Showcase.csproj
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
using Beresta.Markdown.Wpf;
using Beresta.Markdown.Wpf.Editor;
using Beresta.Markdown.Wpf.Extensions;
using Beresta.Markdown.Wpf.Kit;
using Beresta.Markdown.Wpf.Scrolling;
using Beresta.Markdown.Wpf.Search;
using Beresta.Markdown.Wpf.Selection;
using Beresta.Markdown.Wpf.Showcase;
using Beresta.Markdown.Wpf.Showcase.Localization;
using Beresta.Markdown.Wpf.Showcase.Showcases.TwoPanes;
using Polygon = System.Windows.Shapes.Polygon;

const int ExtendedStyle = -20;
const int NoActivate = 0x08000000;
const int ToolWindow = 0x00000080;

var names = new List<string>();
var languages = new List<bool>();
var output = Path.GetFullPath(Path.Combine(ScriptDirectory(), "..", "docs", "wiki", "images"));
for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--out")
    {
        output = args[++i];
    }
    else if (args[i] == "--lang")
    {
        languages.Add(args[++i] == "ru");
    }
    else
    {
        names.AddRange(args[i].Split(',', StringSplitOptions.RemoveEmptyEntries));
    }
}

if (languages.Count == 0)
{
    languages.AddRange([false, true]);
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

    // Снимки каждой вики — в локализации витрины на её языке; свой загрузчик на язык — у баннера своя подпись
    foreach (var russian in languages)
    {
        Localizer.Apply(russian);
        loader = new ShotLoader();
        Light(russian);
    }

    // BYDESIGN: тёмная тема — последней: она переключает тему всего приложения.
    if (Wanted("dark-theme.png"))
    {
        var options = new MainWindowViewModel().Options;
        options.IsDark = true;
        foreach (var russian in languages)
        {
            options.IsRussian = russian;
            loader = new ShotLoader();
            Shot(russian, "dark-theme.png", new MarkdownViewer { Markdown = Overview(russian), ImageLoader = loader }, 760);
        }
    }

    app.Shutdown();

    void Light(bool russian)
    {
        string L(string english, string russianText) => russian ? russianText : english;

        // Вьюер
        if (Wanted("overview.png"))
        {
            Shot(russian, "overview.png", new MarkdownViewer { Markdown = Overview(russian), ImageLoader = loader }, 760);
        }

        if (Wanted("selection.png"))
        {
            var viewer = new MarkdownViewer
            {
                Markdown = L(
                    "Text is selected **across blocks** with the mouse,\nand search results are highlighted with decorations."
                    + "\n\nSelection keeps working in lists:\n\n- found in the first item\n- and in the second item",
                    "Текст выделяется мышью **через блоки**,\nа найденное подсвечивается оформлением."
                    + "\n\nВыделение работает и в списках:\n\n- нашлось в первом пункте\n- и во втором пункте")
            };
            var (from, to, found) = (L("across", "через"), L("decorations", "оформлением"), L("item", "пункте"));
            Shot(russian, "selection.png", viewer, 640, () =>
            {
                var text = viewer.TextMap.Text;
                var start = text.IndexOf(from, StringComparison.Ordinal);
                viewer.Select(start, text.IndexOf(to, StringComparison.Ordinal) - start);
                var decorations = new List<MdDecoration>();
                for (var index = text.IndexOf(found, StringComparison.Ordinal);
                     index >= 0;
                     index = text.IndexOf(found, index + found.Length, StringComparison.Ordinal))
                {
                    decorations.Add(new(new(index, found.Length), new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xC1, 0x07))));
                }

                viewer.Decorations = decorations;
            });
        }

        if (Wanted("line-numbers.png"))
        {
            Shot(russian, "line-numbers.png", new MarkdownViewer
            {
                Markdown = L(
                    "# Line numbers\n\nNumbers follow the source file,\nblank lines keep theirs too.\n\n",
                    "# Номера строк\n\nНомера идут по исходному файлу,\nпустые строки тоже их держат.\n\n")
                    + "```csharp\nint Add(int a, int b)\n{\n    return a + b;\n}\n```\n\n| a | b |\n|---|---|\n| 1 | 2 |",
                ShowSourceLineNumbers = true,
                ShowCodeLineNumbers = true
            }, 640);
        }

        if (Wanted("code-block.png"))
        {
            var viewer = new MarkdownViewer
            {
                Markdown = "```csharp\npublic sealed record Note(string Title, string Text);\n\nvar note = new Note("
                    + L("\"Hello\", \"World\"", "\"Привет\", \"Мир\"") + ");\n```"
            };
            Shot(russian, "code-block.png", viewer, 640, () =>
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
                                    xmlns:mdv="clr-namespace:Beresta.Markdown.Wpf;assembly=Beresta.Markdown.Wpf"
                                    xmlns:p="clr-namespace:Beresta.Markdown.Wpf.Presenters;assembly=Beresta.Markdown.Wpf"
                                    xmlns:ext="clr-namespace:Beresta.Markdown.Wpf.Showcase.Showcases.Extensions;assembly=Beresta.Markdown.Wpf.Showcase">
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
            viewer.Markdown = L(
                "Ask @anna or @boris about custom syntax.\n\n:::note Your own block\nA container with **markdown inside**:"
                + "\n\n- it is selectable\n- and copied with its text\n:::",
                "Спроси @anna или @boris про свой синтаксис.\n\n:::note Свой блок\nКонтейнер с **markdown внутри**:"
                + "\n\n- он выделяется\n- и копируется со своим текстом\n:::");
            Shot(russian, "widgets.png", viewer, 640);
        }

        if (Wanted("custom-template.png"))
        {
            var viewer = (MarkdownViewer)XamlReader.Parse("""
                <mdv:MarkdownViewer xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                                    xmlns:mdv="clr-namespace:Beresta.Markdown.Wpf;assembly=Beresta.Markdown.Wpf"
                                    xmlns:p="clr-namespace:Beresta.Markdown.Wpf.Presenters;assembly=Beresta.Markdown.Wpf"
                                    xmlns:s="clr-namespace:Beresta.Markdown.Wpf.Styling;assembly=Beresta.Markdown.Wpf"
                                    xmlns:vm="clr-namespace:Beresta.Markdown.Wpf.ViewModels;assembly=Beresta.Markdown.Wpf">
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
            viewer.Markdown = L(
                "# Your own heading template\n\nOnly the heading template is replaced: a bar on the left and a serif font."
                + "\nParagraphs, lists and code keep the standard look.\n\n## Second level\n\n- the rest stays default",
                "# Свой шаблон заголовка\n\nЗаменён только шаблон заголовка: полоса слева и шрифт с засечками."
                + "\nАбзацы, списки и код выглядят как обычно.\n\n## Второй уровень\n\n- остальное — по умолчанию");
            Shot(russian, "custom-template.png", viewer, 640);
        }

        // Поиск, пометки, две панели, кнопки на полях
        if (Wanted("search.png"))
        {
            var viewer = new MarkdownViewer
            {
                Markdown = L(
                    "## Search\n\nSearch marks every match of the query and the current one in its own color."
                    + "\nThe application owns the search field; the viewer finds the matches.\n\n"
                    + "- matches are found in the whole document\n- the current match is the one the viewer scrolls to",
                    "## Поиск\n\nПоиск помечает каждое совпадение с запросом, а текущее — своим цветом."
                    + "\nПоле поиска — у приложения, совпадения находит вьюер.\n\n"
                    + "- совпадения ищутся по всему документу\n- текущее совпадение — то, к которому прокручивает вьюер")
            };
            var query = L("match", "совпаден");
            Shot(russian, "search.png", viewer, 640, () => new MdSearch(viewer) { Query = query }.Next());
        }

        if (Wanted("marks.png"))
        {
            var viewer = new MarkdownViewer
            {
                Markdown = L(
                    "## Marks\n\nA fill paints a piece of text, a line underlines a comment,\nand a frame draws attention to a word."
                    + "\nA caret stands where the other version has text.\n\nA row spans the whole width of the document."
                    + "\n\nBars and icons stand in the margin next to the line numbers.",
                    "## Пометки\n\nЗаливка закрашивает кусок текста, линия подчёркивает комментарий,\nа рамка привлекает внимание к слову."
                    + "\nВертикальная черта стоит там, где у другой версии есть текст.\n\nСтрока тянется на всю ширину документа."
                    + "\n\nПолосы и значки стоят на полях рядом с номерами строк."),
                ShowSourceLineNumbers = true
            };
            var (fill, comment, frame, caret) = (L("fill", "Заливка"), L("a comment", "комментарий"), L("a word", "слову"), L("where", "где"));
            Shot(russian, "marks.png", viewer, 640, () =>
            {
                var text = viewer.TextMap.Text;
                MdTextRange Word(string word) => new(text.IndexOf(word, StringComparison.Ordinal), word.Length);
                viewer.MarkLayers.Add(new MdMarkLayer("Demo")
                {
                    Marks =
                    [
                        new MdMark(Word(fill)) { Style = "Found" },
                        new MdMark(Word(comment), MdMarkKind.Underline) { Style = "Comment" },
                        new MdMark(Word(frame), MdMarkKind.Frame) { Style = "Changed" },
                        new MdMark(new MdTextRange(Word(caret).Start, 0), MdMarkKind.Caret) { Style = "Removed" },
                        new MdMark(6, 6) { Style = "Added" },
                        new MdMark(2, 4, MdMarkKind.Bar) { Style = "Changed" },
                        new MdMark(6, 6, MdMarkKind.Bar) { Style = "Added" },
                        new MdMark(8, 8, MdMarkKind.Icon) { Style = "Comment", Icon = Geometry.Parse("M1,1 H13 V9 H6 L3,12 V9 H1 Z") }
                    ]
                });
            });
        }

        var (leftText, rightText) = PaneTexts(russian);
        if (Wanted("two-panes.png"))
        {
            var (left, right, grid) = Panes(leftText, rightText);
            Shot(russian, "two-panes.png", grid, 900, () => new MdPaneAligner(left, right, Diff(left, right, leftText, rightText)).Align());
        }

        if (Wanted("two-panes-connections.png"))
        {
            var (left, right, grid) = Panes(leftText, rightText);
            var connections = (Canvas)grid.Children[1];
            Shot(russian, "two-panes-connections.png", grid, 900, () =>
            {
                Diff(left, right, leftText, rightText);
                var leftLines = leftText.Split('\n');
                var rightLines = rightText.Split('\n');
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
                Markdown = L(
                    "## Bookmarks\n\nThe column on the left belongs to the application.\nIts buttons stand at the source lines in view."
                    + "\n\nA click makes a line a bookmark,\nand the application marks it across the whole width.\n\n",
                    "## Закладки\n\nКолонка слева принадлежит приложению.\nЕё кнопки стоят у видимых строк исходника."
                    + "\n\nЩелчок делает строку закладкой,\nи приложение помечает её во всю ширину.\n\n")
                    + "```csharp\nvar rows = Viewer.GetCoordinates(Marks).VisibleRows();\n```"
            };
            var column = new Canvas();
            var grid = new Grid { ColumnDefinitions = { new() { Width = new(28) }, new() } };
            Grid.SetColumn(viewer, 1);
            grid.Children.Add(column);
            grid.Children.Add(viewer);
            Shot(russian, "margin-buttons.png", grid, 640, () =>
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

        // Редактор: блочный режим с курсором в абзаце и в пункте списка, весь текст и «только контролы» с полями карточки
        var notes = L("""
            ---
            title: Release notes
            tags: [editor, markdown]
            ---

            ## What's new

            The **editor** shows the block under the caret as its source,
            and the rest of the document stays drawn.

            - [x] Blocks under the caret
            - [ ] The whole text in place
            """, """
            ---
            title: Заметки к выпуску
            tags: [редактор, markdown]
            ---

            ## Что нового

            **Редактор** показывает блок под курсором исходником,
            а остальной документ остаётся нарисованным.

            - [x] Блоки под курсором
            - [ ] Весь текст на месте
            """).ReplaceLineEndings("\n");
        foreach (var (name, mode) in new[] { ("editor-blocks.png", MdEditMode.Blocks), ("editor-item.png", MdEditMode.Blocks), ("editor-full.png", MdEditMode.Full), ("editor-controls.png", MdEditMode.Controls) })
        {
            if (!Wanted(name))
            {
                continue;
            }

            var editor = new MarkdownEditor { Markdown = notes, EditMode = mode };
            editor.Extensions.Add(new PropertiesCard());
            Shot(russian, name, editor, 640, () =>
            {
                if (name == "editor-item.png")
                {
                    editor.SourceCaretOffset = notes.IndexOf(L("The whole text", "Весь текст"), StringComparison.Ordinal) + 4;
                }
                else if (mode == MdEditMode.Blocks)
                {
                    editor.SourceCaretOffset = notes.IndexOf(L("The **editor**", "**Редактор**"), StringComparison.Ordinal) + 4;
                }
            });
        }

        // Готовые расширения
        var github = new GitHubSet { Resolver = resolver };
        github.IssueTemplates["#"] = "https://github.com/org/repo/issues/{0}";
        Kit(russian, "kit-github.png", github, L("""
            ---
            title: Release notes
            version: 0.6.0
            ---

            Thanks @anna for #12 and @boris for #15 :tada:

            The accent color is `#0969DA`, the warning one is `rgb(154, 103, 0)`.
            """, """
            ---
            title: Заметки к выпуску
            version: 0.6.0
            ---

            Спасибо @anna за #12 и @boris за #15 :tada:

            Цвет акцента — `#0969DA`, предупреждения — `rgb(154, 103, 0)`.
            """));

        var gitlab = new GitLabSet();
        gitlab.IssueTemplates["#"] = "https://gitlab.com/group/project/-/issues/{0}";
        Kit(russian, "kit-gitlab.png", gitlab, L("""
            [[_TOC_]]

            ## Staging

            Fixes #42 :rocket: — the new badge color is `#FC6D26`.

            ## Production

            Released after the review :white_check_mark:
            """, """
            [[_TOC_]]

            ## Стенд

            Исправляет #42 :rocket: — новый цвет значка `#FC6D26`.

            ## Прод

            Выкатили после ревью :white_check_mark:
            """));

        Kit(russian, "kit-obsidian.png", new ObsidianSet { Resolver = resolver }, L("""
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
            """, """
            ---
            tags: [проект, wpf]
            status: черновик
            ---

            > [!tip] Ежедневная заметка
            > Ссылки на [[Дорожная карта]], [[Несуществующая страница]] и раздел [[#Задачи]].

            Тег #идея, %%скрытый комментарий%% и встроенная картинка:

            ![[banner.png|240]]

            ## Задачи

            - [/] в работе
            - [>] перенесена
            - [!] важная
            """));

        Kit(russian, "kit-mkdocs.png", new MkDocsSet(), L("""
            !!! note "Install"
                Run `dotnet add package Beresta.Markdown.Wpf`.

            ??? warning "Collapsed at first"
                Click the title to read.

            ???+ example "Open at first"
                A collapsible admonition that starts open.
            """, """
            !!! note "Установка"
                Выполни `dotnet add package Beresta.Markdown.Wpf`.

            ??? warning "Свёрнуто сразу"
                Щёлкни заголовок, чтобы прочитать.

            ???+ example "Развёрнуто сразу"
                Сворачиваемое предупреждение, открытое с начала.
            """));

        Kit(russian, "kit-docusaurus.png", new DocusaurusSet(), L("""
            :::tip Pro tip
            Containers take a kind and a title.
            :::

            :::danger[A title in brackets]
            As in Docusaurus 3.
            :::

            :::details Click to expand
            A collapsible block from VuePress.
            :::
            """, """
            :::tip Совет
            У контейнера вид и заголовок.
            :::

            :::danger[Заголовок в скобках]
            Как в Docusaurus 3.
            :::

            :::details Щёлкни, чтобы развернуть
            Сворачиваемый блок из VuePress.
            :::
            """));

        Kit(russian, "kit-hackmd.png", new HackMDSet(), L("""
            [TOC]

            ## Meeting notes

            :::success
            Shipped on Friday!
            :::

            {%youtube jNQXAC9IVRw %}

            :::spoiler Open questions
            Hidden until expanded.
            :::
            """, """
            [TOC]

            ## Заметки встречи

            :::success
            Выпустили в пятницу!
            :::

            {%youtube jNQXAC9IVRw %}

            :::spoiler Открытые вопросы
            Видно, когда развернёшь.
            :::
            """));

        Kit(russian, "kit-chat.png", new ChatSet { Resolver = resolver }, L("""
            @anna the build is green :white_check_mark:

            The answer is ||42|| :wink:
            """, """
            @anna сборка зелёная :white_check_mark:

            Ответ — ||42|| :wink:
            """));

        var callouts = new MdExtensionSet { Extensions = { new ObsidianCallouts(), new MkDocsAdmonitions(), new CalloutContainers() } };
        Kit(russian, "kit-callouts.png", callouts, L("""
            > [!question]- Obsidian, collapsed
            > Hidden until expanded.

            > [!success] Obsidian with a title
            > Any blocks inside.

            !!! bug "MkDocs"
                Content is indented by four spaces.

            :::info Docusaurus and HackMD
            A container with a kind and a title.
            :::
            """, """
            > [!question]- Obsidian, свёрнута
            > Видно, когда развернёшь.

            > [!success] Obsidian с заголовком
            > Внутри — любые блоки.

            !!! bug "MkDocs"
                Содержимое — под отступом в четыре пробела.

            :::info Docusaurus и HackMD
            Контейнер с видом и заголовком.
            :::
            """));

        var issues = new IssueLinks();
        issues.Templates["#"] = "https://github.com/org/repo/issues/{0}";
        var linkExtensions = new MdExtensionSet
        {
            Extensions = { new TableOfContents(), new WikiLinks { Resolver = resolver }, new Tags(), new Mentions { Resolver = resolver }, issues }
        };
        Kit(russian, "kit-links.png", linkExtensions, L("""
            [TOC]

            ## Links

            A wiki link to [[Roadmap]], a missing [[Missing page]], a tag #release, a mention @anna
            and issue #12.

            ## Table of contents

            Follows the headings as the text changes.
            """, """
            [TOC]

            ## Ссылки

            Вики-ссылка на [[Дорожная карта]], отсутствующая [[Несуществующая страница]], тег #релиз,
            упоминание @anna и задача #12.

            ## Оглавление

            Следует за заголовками, пока текст меняется.
            """));

        var textExtensions = new MdExtensionSet
        {
            Extensions = { new Chips(), new ColorChips(), new CriticMarkup(), new Spoilers(), new TaskStatuses(), new FootnoteTips() }
        };
        Kit(russian, "kit-text.png", textExtensions, L("""
            Status: ::done::{.success} ::review::{.info} ::risk::{.warning} ::blocked::{.danger}, color `#8250DF`.

            Text {++added++}, {--removed--}, {~~old~>new~~} and {==marked==}{>>a comment<<}.

            A ||spoiler||, a footnote in a tooltip[^1].

            - [/] in progress
            - [?] in question

            [^1]: The note.
            """, """
            Статус: ::готово::{.success} ::ревью::{.info} ::риск::{.warning} ::стоп::{.danger}, цвет `#8250DF`.

            Текст {++добавлен++}, {--удалён--}, {~~было~>стало~~} и {==выделен==}{>>комментарий<<}.

            ||Спойлер||, сноска подсказкой[^1].

            - [/] в работе
            - [?] под вопросом

            [^1]: Сноска.
            """));

        var mediaExtensions = new MdExtensionSet
        {
            Extensions = { new PropertiesCard(), new VideoCards(), new EmojiShortcodes(), new Shortcodes(), new CsvTables() }
        };
        Kit(russian, "kit-media.png", mediaExtensions, L("""
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
            """, """
            ---
            title: Медиа и данные
            tags: [видео, csv]
            ---

            [![Карточка видео](cover.png)](https://www.youtube.com/watch?v=jNQXAC9IVRw)

            {%pdf "https://example.com/guide.pdf" %} :movie_camera:

            ```csv
            Город,Население,"Площадь, км²"
            Москва,13 010 112,2561
            Новосибирск,1 633 595,502
            ```
            """));
    }

    void Kit(bool russian, string name, MdExtension extension, string markdown)
    {
        if (!Wanted(name))
        {
            return;
        }

        var viewer = new MarkdownViewer { Markdown = markdown.ReplaceLineEndings("\n"), ImageLoader = loader };
        viewer.Extensions.Add(extension);
        Shot(russian, name, viewer, 640);
    }

    void Shot(bool russian, string name, FrameworkElement content, double width, Action? prepare = null)
    {
        var frame = new Border
        {
            Width = width,
            Padding = new(20, 16, 20, 16),
            Child = content
        };
        frame.SetResourceReference(Border.BackgroundProperty, "RegionBrush");
        frame.SetResourceReference(TextElement.ForegroundProperty, "PrimaryTextBrush");

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
        var file = russian ? Path.ChangeExtension(name, null) + "-ru.png" : name;
        using (var stream = File.Create(Path.Combine(output, file)))
        {
            encoder.Save(stream);
        }

        Console.WriteLine($"снимок: {file} {bitmap.PixelWidth}x{bitmap.PixelHeight}");
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

static string Overview(bool russian) => russian
    ? """
      # Beresta.Markdown.Wpf

      ![Баннер](banner.png)

      **Контрол WPF**, который рисует *Markdown*: не ~~HTML~~, а родные элементы WPF, `виртуализированно` и с выделением.
      Ссылки вроде [репозитория](https://github.com/porohkun/Beresta.Markdown.Wpf) открываются в браузере.

      ## Списки и задачи

      - первый пункт
      - второй пункт
        - вложенный пункт
      - [x] сделанная задача
      - [ ] открытая задача

      ## Таблица

      | Возможность | Состояние | С версии |
      |:--------|:------:|------:|
      | Выделение | готово | 0.3 |
      | Сноски | готово | 0.5 |

      > Цитаты держат свои блоки, а сноски стоят там, где написаны[^1].

      [^1]: Как эта.

      ```csharp
      var viewer = new MarkdownViewer { Markdown = text };
      ```

      <kbd>Ctrl</kbd>+<kbd>C</kbd> копирует, H<sub>2</sub>O и E = mc<sup>2</sup> тоже работают.
      """
    : """
      # Beresta.Markdown.Wpf

      ![Banner](banner.png)

      A **WPF control** that renders *Markdown*: ~~HTML~~ native WPF elements, `virtualized` and selectable.
      Links like [the repository](https://github.com/porohkun/Beresta.Markdown.Wpf) open in the browser.

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

static (string Left, string Right) PaneTexts(bool russian) => russian
    ? ("# Заметки к выпуску\n\nВьюер рисует markdown.\n\nОн поддерживает таблицы.\n\nКопирование кладёт обычный текст.",
       "# Заметки к выпуску\n\nВьюер рисует markdown.\n\nОн поддерживает таблицы и сноски.\n\n"
       + "Две панели стоят рядом,\nа напротив правок — пустые места.\n\nКопирование кладёт обычный текст.")
    : ("# Release notes\n\nThe viewer renders markdown.\n\nIt supports tables.\n\nCopying puts plain text.",
       "# Release notes\n\nThe viewer renders markdown.\n\nIt supports tables and footnotes.\n\n"
       + "Two panes stand side by side\nwith empty places opposite the changes.\n\nCopying puts plain text.");

static (MarkdownViewer Left, MarkdownViewer Right, Grid Grid) Panes(string leftText, string rightText)
{
    var left = new MarkdownViewer { Markdown = leftText };
    var right = new MarkdownViewer { Markdown = rightText };
    var grid = new Grid { ColumnDefinitions = { new(), new() { Width = new(48) }, new() } };
    Grid.SetColumn(right, 2);
    grid.Children.Add(left);
    var connections = new Canvas { ClipToBounds = true };
    Grid.SetColumn(connections, 1);
    grid.Children.Add(connections);
    grid.Children.Add(right);
    return (left, right, grid);
}

static MdLineMap Diff(MarkdownViewer left, MarkdownViewer right, string leftText, string rightText)
{
    var leftLines = leftText.Split('\n');
    var rightLines = rightText.Split('\n');
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
    public Task<ImageSource?> LoadAsync(string url, Uri? uri, int decodeWidth, CancellationToken cancellationToken)
        => Task.FromResult<ImageSource?>(url.Contains("banner", StringComparison.Ordinal) ? Banner() : Still());

    private static ImageSource Banner()
    {
        const int width = 640;
        const int height = 160;
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            var background = new LinearGradientBrush(Color.FromRgb(0x4A, 0x31, 0x22), Color.FromRgb(0x94, 0x5E, 0x33), 20);
            context.DrawRoundedRectangle(background, null, new Rect(0, 0, width, height), 16, 16);
            context.DrawImage(Icon(), new Rect(20, 12, 136, 136));
            context.DrawText(Text("Beresta.Markdown.Wpf", FontWeights.SemiBold, 36, new SolidColorBrush(Color.FromRgb(0xFF, 0xF8, 0xEC))), new Point(176, 42));
            var caption = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru" ? "Markdown для WPF" : "Markdown for WPF";
            context.DrawText(Text(caption, FontWeights.Normal, 22, new SolidColorBrush(Color.FromRgb(0xEE, 0xD9, 0xBC))), new Point(178, 90));
        }

        return Render(visual, width, height);
    }

    /// <summary>Значок пакета из корня репозитория.</summary>
    private static ImageSource Icon([CallerFilePath] string script = "")
    {
        var icon = new BitmapImage();
        icon.BeginInit();
        icon.UriSource = new Uri(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(script)!, "..", "icon512.png")));
        icon.CacheOption = BitmapCacheOption.OnLoad;
        icon.EndInit();
        icon.Freeze();
        return icon;
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

/// <summary>Страницы и люди снимков на текущем языке: нет только несуществующей страницы.</summary>
sealed class ShotResolver : IMdLinkResolver
{
    public bool PageExists(string page) => page is not ("Missing page" or "Несуществующая страница");

    public string? DisplayName(string user) => (user, CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru") switch
    {
        ("anna", false) => "Anna Smith",
        ("anna", true) => "Анна Смирнова",
        ("boris", false) => "Boris Ivanov",
        ("boris", true) => "Борис Иванов",
        _ => null
    };
}
