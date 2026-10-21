namespace Beresta.Markdown.Wpf.Tests;

using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Extensions;
using Kit;
using Markdig.Extensions.TaskLists;
using Presenters;
using ViewModels;

/// <summary>Флажки задач и сворачивание пишут в исходник, пока контролы правят исходник.</summary>
public sealed class SourceEditTests
{
    [Fact]
    public void Флажок_задачи_ставит_и_снимает_отметку()
    {
        WpfHost.Invoke(() =>
        {
            var (viewer, window) = Show("- [ ] one\n- [X] two");

            Toggle(viewer, window, 0);
            viewer.Markdown.Should().Be("- [x] one\n- [X] two");
            Toggle(viewer, window, 1);
            viewer.Markdown.Should().Be("- [x] one\n- [ ] two");
            window.Close();
        });
    }

    [Fact]
    public void Без_правки_контролами_флажок_только_картинка()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "- [ ] one" };
            var window = WpfHost.Show(viewer);
            var peer = UIElementAutomationPeer.CreatePeerForElement(Visuals.Descendants<MdTaskMark>(viewer).Single());
            var toggle = (IToggleProvider)peer.GetPattern(PatternInterface.Toggle);

            peer.IsEnabled().Should().BeFalse();
            toggle.Invoking(provider => provider.Toggle()).Should().Throw<ElementNotEnabledException>();
            viewer.Markdown.Should().Be("- [ ] one");
            window.Close();
        });
    }

    [Fact]
    public void Команда_нажатия_доступна_только_пока_контролы_правят_исходник()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "- [ ] one" };
            var window = WpfHost.Show(viewer);
            var mark = Visuals.Descendants<MdTaskMark>(viewer).Single();

            MarkdownViewer.ActivateWidgetCommand.CanExecute(mark.DataContext, mark).Should().BeFalse();
            viewer.AreControlsEditable = true;
            MarkdownViewer.ActivateWidgetCommand.CanExecute(mark.DataContext, mark).Should().BeTrue();
            MarkdownViewer.ActivateWidgetCommand.Execute(mark.DataContext, mark);
            viewer.Markdown.Should().Be("- [x] one");
            window.Close();
        });
    }

    [Fact]
    public void Взятая_приложением_правка_в_текст_не_ложится()
    {
        WpfHost.Invoke(() =>
        {
            var (viewer, window) = Show("- [ ] one");
            var requests = new List<MdSourceEditRequestedEventArgs>();
            viewer.SourceEditRequested += (_, e) =>
            {
                requests.Add(e);
                e.Handled = true;
            };

            Toggle(viewer, window, 0);

            requests.Should().ContainSingle();
            requests[0].Edit.Should().Be(new MdSourceEdit(3, 1, "x"));
            requests[0].Node.Should().BeOfType<TaskVm>();
            viewer.Markdown.Should().Be("- [ ] one");
            window.Close();
        });
    }

    [Theory]
    [InlineData("<details>\n<summary>S</summary>\n\nbody\n\n</details>", "<details open>\n<summary>S</summary>\n\nbody\n\n</details>")]
    [InlineData("<details class=\"a\">\n<summary>S</summary>\n\nbody\n\n</details>", "<details class=\"a\" open>\n<summary>S</summary>\n\nbody\n\n</details>")]
    public void Разворачивание_details_пишет_open(string markdown, string expected)
    {
        WpfHost.Invoke(() =>
        {
            var (viewer, window) = Show(markdown);

            Fold<MdDetailsPresenter>(viewer, window, expand: true);
            viewer.Markdown.Should().Be(expected);
            window.Close();
        });
    }

    [Theory]
    [InlineData("<details open>\n<summary>S</summary>\n\nbody\n\n</details>", "<details>\n<summary>S</summary>\n\nbody\n\n</details>")]
    [InlineData("<details open=\"\">\n<summary>S</summary>\n\nbody\n\n</details>", "<details>\n<summary>S</summary>\n\nbody\n\n</details>")]
    [InlineData("<details class=\"a\" OPEN=\"open\" id=\"b\">\n<summary>S</summary>\n\nbody\n\n</details>", "<details class=\"a\" id=\"b\">\n<summary>S</summary>\n\nbody\n\n</details>")]
    public void Сворачивание_details_убирает_open(string markdown, string expected)
    {
        WpfHost.Invoke(() =>
        {
            var (viewer, window) = Show(markdown);

            Fold<MdDetailsPresenter>(viewer, window, expand: false);
            viewer.Markdown.Should().Be(expected);
            window.Close();
        });
    }

    [Fact]
    public void Плашка_Obsidian_пишет_знак_свёрнутости()
    {
        WpfHost.Invoke(() =>
        {
            var (viewer, window) = Show("> [!note]- Title\n> body", new ObsidianCallouts());

            Fold<MdCalloutPresenter>(viewer, window, expand: true);
            viewer.Markdown.Should().Be("> [!note]+ Title\n> body");
            Fold<MdCalloutPresenter>(viewer, window, expand: false);
            viewer.Markdown.Should().Be("> [!note]- Title\n> body");
            window.Close();
        });
    }

    [Fact]
    public void Предупреждение_MkDocs_пишет_плюс()
    {
        WpfHost.Invoke(() =>
        {
            var (viewer, window) = Show("??? note \"Title\"\n    body", new MkDocsAdmonitions());

            Fold<MdCalloutPresenter>(viewer, window, expand: true);
            viewer.Markdown.Should().Be("???+ note \"Title\"\n    body");
            Fold<MdCalloutPresenter>(viewer, window, expand: false);
            viewer.Markdown.Should().Be("??? note \"Title\"\n    body");
            window.Close();
        });
    }

    [Theory]
    [InlineData(":::spoiler Title\nbody\n:::", ":::spoiler {state=\"open\"} Title\nbody\n:::")]
    [InlineData(":::details[Title]\nbody\n:::", ":::details[Title] {state=\"open\"}\nbody\n:::")]
    [InlineData(":::details {.wide} Title\nbody\n:::", ":::details {.wide state=\"open\"} Title\nbody\n:::")]
    public void Контейнер_пишет_state_open_и_убирает_его(string markdown, string expanded)
    {
        WpfHost.Invoke(() =>
        {
            var (viewer, window) = Show(markdown, new CalloutContainers());

            Fold<MdDetailsPresenter>(viewer, window, expand: true);
            viewer.Markdown.Should().Be(expanded);
            Fold<MdDetailsPresenter>(viewer, window, expand: false);
            viewer.Markdown.Should().Be(markdown);
            window.Close();
        });
    }

    [Fact]
    public void Статус_задачи_перебирается_по_кругу()
    {
        WpfHost.Invoke(() =>
        {
            var (viewer, window) = Show("- [/] task", new TaskStatuses());
            var seen = new List<string>();

            for (var i = 0; i < 5; i++)
            {
                Toggle(viewer, window, 0);
                seen.Add(viewer.Markdown![3..4]);
            }

            seen.Should().Equal("-", ">", "?", "!", "/");
            window.Close();
        });
    }

    [Fact]
    public void Сбой_расширения_уходит_в_RenderFailed_а_текст_не_меняется()
    {
        WpfHost.Invoke(() =>
        {
            var failing = new LambdaExtension
            {
                Inline = (inline, _) => inline is TaskList task ? new TaskVm(task.Checked) : null,
                Edit = (_, _, _) => throw new InvalidOperationException("edit")
            };
            var (viewer, window) = Show("- [ ] one", failing);
            var failures = new List<MdRenderFailedEventArgs>();
            viewer.RenderFailed += (_, e) => failures.Add(e);

            Toggle(viewer, window, 0);
            WpfHost.Settle(window);

            failures.Should().ContainSingle().Which.Extension.Should().BeSameAs(failing);
            viewer.Markdown.Should().Be("- [ ] one");
            window.Close();
        });
    }

    [Fact]
    public void Свёрнутость_следует_исходнику_пока_контролы_правят_его()
    {
        WpfHost.Invoke(() =>
        {
            var (viewer, window) = Show("<details open>\n<summary>S</summary>\n\nbody\n\n</details>");
            Visuals.Descendants<MdDetailsPresenter>(viewer).Single().IsExpanded.Should().BeTrue();

            viewer.Markdown = "<details>\n<summary>S</summary>\n\nbody\n\n</details>";
            WpfHost.Settle(window);

            Visuals.Descendants<MdDetailsPresenter>(viewer).Single().IsExpanded.Should().BeFalse();
            viewer.Markdown.Should().Be("<details>\n<summary>S</summary>\n\nbody\n\n</details>");
            window.Close();
        });
    }

    private static (MarkdownViewer Viewer, Window Window) Show(string markdown, params MdExtension[] extensions)
    {
        var viewer = new MarkdownViewer { Markdown = markdown, AreControlsEditable = true };
        foreach (var extension in extensions)
        {
            viewer.Extensions.Add(extension);
        }

        return (viewer, WpfHost.Show(viewer));
    }

    private static void Toggle(MarkdownViewer viewer, Window window, int index)
    {
        var mark = Visuals.Descendants<MdTaskMark>(viewer).ElementAt(index);
        var peer = UIElementAutomationPeer.CreatePeerForElement(mark);
        ((IToggleProvider)peer.GetPattern(PatternInterface.Toggle)).Toggle();
        WpfHost.Settle(window);
    }

    private static void Fold<T>(MarkdownViewer viewer, Window window, bool expand)
        where T : FrameworkElement
    {
        var presenter = Visuals.Descendants<T>(viewer).Single();
        var expander = (IExpandCollapseProvider)UIElementAutomationPeer.CreatePeerForElement(presenter).GetPattern(PatternInterface.ExpandCollapse);
        if (expand)
        {
            expander.Expand();
        }
        else
        {
            expander.Collapse();
        }

        WpfHost.Settle(window);
    }
}
