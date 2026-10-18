namespace MarkdigWpfViewer.Tests;

using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Markup;
using Extensions;
using Kit;
using Markdig.Syntax;
using ViewModels;
using static TestWidgets;

/// <summary>Общие расширения нескольких вьюеров рядом со своими.</summary>
public sealed class SharedExtensionsTests
{
    [Fact]
    public void Общая_коллекция_не_держит_вьюеры()
    {
        WpfHost.Invoke(() =>
        {
            var shared = new MdExtensionCollection { new Tags() };
            var panel = new StackPanel();
            var window = WpfHost.Show(panel);
            var viewers = AddViewers(panel, shared, 20);
            WpfHost.Settle(window);

            panel.Children.Clear();
            WpfHost.Settle(window);
            window.Close();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            viewers.Count(viewer => viewer.IsAlive).Should().Be(0);
            GC.KeepAlive(shared);
        });
    }

    [Fact]
    public void Свои_расширения_из_XAML_не_попадают_в_общую_коллекцию()
    {
        const string xaml = """
            <StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        xmlns:mdv="clr-namespace:MarkdigWpfViewer;assembly=Markdig.Wpf.Viewer"
                        xmlns:mdx="clr-namespace:MarkdigWpfViewer.Extensions;assembly=Markdig.Wpf.Viewer"
                        xmlns:kit="clr-namespace:MarkdigWpfViewer.Kit;assembly=Markdig.Wpf.Viewer">
                <StackPanel.Resources>
                    <mdx:MdExtensionCollection x:Key="Shared">
                        <kit:Tags />
                    </mdx:MdExtensionCollection>
                    <Style x:Key="Viewer" TargetType="mdv:MarkdownViewer" BasedOn="{StaticResource {x:Type mdv:MarkdownViewer}}">
                        <Setter Property="SharedExtensions" Value="{StaticResource Shared}" />
                    </Style>
                </StackPanel.Resources>
                <mdv:MarkdownViewer Style="{StaticResource Viewer}" Markdown="text #tag ||secret||">
                    <mdv:MarkdownViewer.Extensions>
                        <kit:Spoilers />
                    </mdv:MarkdownViewer.Extensions>
                </mdv:MarkdownViewer>
                <mdv:MarkdownViewer Style="{StaticResource Viewer}" Markdown="text #tag ||secret||">
                    <mdv:MarkdownViewer.Extensions>
                        <kit:Spoilers />
                    </mdv:MarkdownViewer.Extensions>
                </mdv:MarkdownViewer>
            </StackPanel>
            """;

        WpfHost.Invoke(() =>
        {
            var panel = (StackPanel)XamlReader.Parse(xaml);
            var window = WpfHost.Show(panel);

            ((MdExtensionCollection)panel.Resources["Shared"]).Should().ContainSingle().Which.Should().BeOfType<Tags>();
            foreach (var viewer in panel.Children.OfType<MarkdownViewer>())
            {
                viewer.Extensions.Should().ContainSingle().Which.Should().BeOfType<Spoilers>();
                viewer.Items.OfType<ParagraphVm>().Single().Inlines.OfType<RunVm>().Select(run => run.StyleName)
                    .Should().Contain(["Tag", "Spoiler"]);
            }

            window.Close();
        });
    }

    [Fact]
    public void Свои_расширения_опрашиваются_раньше_общих()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { SharedExtensions = [Claiming("shared")], Markdown = "text" };
            viewer.Extensions.Add(Claiming("own"));
            var window = WpfHost.Show(viewer);

            viewer.Items.OfType<DiagramVm>().Single().Code.Should().Be("own");
            window.Close();
        });
    }

    [Fact]
    public void Общая_коллекция_с_панели_доходит_до_вьюеров_и_следит_за_правками()
    {
        WpfHost.Invoke(() =>
        {
            var shared = new MdExtensionCollection();
            var viewer = new MarkdownViewer { Markdown = "text" };
            var panel = new StackPanel { Children = { viewer } };
            MarkdownViewer.SetSharedExtensions(panel, shared);
            var window = WpfHost.Show(panel);
            viewer.Items.OfType<ParagraphVm>().Should().ContainSingle();

            shared.Add(Claiming("shared"));

            viewer.Items.OfType<DiagramVm>().Single().Code.Should().Be("shared");
            window.Close();
        });
    }

    /// <summary>Вьюеры с общей коллекцией в панели; ссылки на них — слабые.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static List<WeakReference> AddViewers(Panel panel, MdExtensionCollection shared, int count)
    {
        var viewers = Enumerable.Range(0, count)
            .Select(_ => new MarkdownViewer { SharedExtensions = shared, Markdown = "text #tag" })
            .ToList();

        foreach (var viewer in viewers)
        {
            panel.Children.Add(viewer);
        }

        return viewers
            .Select(viewer => new WeakReference(viewer))
            .ToList();
    }

    /// <summary>Расширение, которое забирает каждый абзац себе.</summary>
    private static LambdaExtension Claiming(string code)
        => new() { Block = (block, _) => block is ParagraphBlock ? new DiagramVm(code) : null };
}
