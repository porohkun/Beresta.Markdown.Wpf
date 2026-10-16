namespace MarkdigWpfViewer.Tests;

using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using Extensions;
using Markdig.Extensions.CustomContainers;
using Presenters;
using Selection;
using ViewModels;

/// <summary>API расширений: нажатие ссылки из виджета, оформленный текст, несколько инлайнов из узла, наборы.</summary>
public sealed class ExtensionApiTests
{
    private const string Chipped = "a ::chip **bold**:: b";

    [Fact]
    public void Кнопка_виджета_нажимает_ссылку_тем_же_путём_что_текст()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ":::note T\nx\n:::" };
            viewer.Extensions.Add(new LambdaExtension
            {
                Block = (block, _) => block is CustomContainer { Info: "note" } ? new TestWidgets.NoteVm("T", []) : null,
                Template = item => item is TestWidgets.NoteVm
                    ? TestWidgets.Template("<Button Command='{x:Static mdv:MarkdownLink.ActivateCommand}' CommandParameter='note:plan' />")
                    : null
            });
            var window = WpfHost.Show(viewer);
            var heard = new List<(string Href, object Source)>();
            window.AddHandler(MarkdownLink.NavigateEvent, new MarkdownLinkEventHandler((_, e) =>
            {
                heard.Add((e.Href, e.OriginalSource));
                e.Handled = true;
            }));
            var button = Visuals.Descendants<Button>(viewer).Single();

            ((IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)).Invoke();
            WpfHost.Settle(window);
            MarkdownLink.Activate(button, "note:direct");

            heard.Should().Equal(("note:plan", button), ("note:direct", button));
            window.Close();
        });
    }

    [Fact]
    public void Оформленный_текст_остаётся_текстом_карты_и_пишется_своим_обрамлением()
    {
        var blocks = ModelBuilder.Parse(Chipped, ModelDump.Pipeline(Chips()), [Chips()]);
        var runs = blocks.OfType<ParagraphVm>().Single().Inlines.OfType<RunVm>().Where(run => run.StyleName == "Chip").ToList();
        var map = MdTextMap.Build(blocks);

        runs.Select(run => (run.Text, run.Bold)).Should().Equal(("chip ", false), ("bold", true));
        runs.Select(run => run.NodeId).Should().OnlyHaveUniqueItems().And.NotContain(0);
        map.Text.Should().Be("a chip bold b");
        MdMarkdownWriter.Write(blocks, map, Chipped, new(0, map.Length)).Should().Be(Chipped);
    }

    [Fact]
    public void Оформленный_текст_рисуется_стилем_из_темы_по_имени()
    {
        WpfHost.Invoke(() =>
        {
            var style = new Style(typeof(Span)) { Setters = { new Setter(TextElement.BackgroundProperty, Brushes.LightGreen) } };
            var viewer = new MarkdownViewer { Markdown = Chipped };
            viewer.Resources["Md.Chip.Style"] = style;
            viewer.Extensions.Add(Chips());
            var window = WpfHost.Show(viewer);
            var text = Visuals.Descendants<MdTextBlock>(viewer).Single();

            Spans(text.Inlines).Count(span => ReferenceEquals(span.Style, style)).Should().Be(2);
            window.Close();
        });
    }

    [Fact]
    public void Пустой_список_инлайнов_прячет_узел()
    {
        var hide = new LambdaExtension { Inlines = (inline, _) => inline is CustomContainerInline ? [] : null };

        ModelDump.Of(Chipped, hide).Should().Be("P(a  b)");
    }

    [Fact]
    public void Набор_опрашивает_свои_расширения_по_порядку()
    {
        var set = new MdExtensionSet
        {
            Extensions =
            {
                new LambdaExtension { Block = (block, _) => block is CustomContainer { Info: "a" } ? new TestWidgets.DiagramVm("first") : null },
                new LambdaExtension { Block = (block, _) => block is CustomContainer ? new TestWidgets.DiagramVm("second") : null },
                Chips()
            }
        };

        var blocks = ModelBuilder.Parse(":::a\nx\n:::\n\n:::b\ny\n:::\n\n" + Chipped, ModelDump.Pipeline(set), [set]);

        blocks.OfType<TestWidgets.DiagramVm>().Select(diagram => diagram.Code).Should().Equal("first", "second");
        blocks.OfType<ParagraphVm>().Single().Inlines.OfType<RunVm>().Should().Contain(run => run.StyleName == "Chip");
    }

    [Fact]
    public void Набор_подключается_вперемешку_с_одиночными_и_собирается_в_XAML()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = ":::note T\nx\n:::\n\n" + Chipped };
            viewer.Extensions.Add(new MdExtensionSet { Extensions = { Chips() } });
            viewer.Extensions.Add(TestWidgets.Note());
            var window = WpfHost.Show(viewer);

            viewer.Items.OfType<TestWidgets.NoteVm>().Should().ContainSingle();
            viewer.Items.OfType<ParagraphVm>().Single().Inlines.OfType<RunVm>().Should().Contain(run => run.StyleName == "Chip");
            window.Close();

            var parsed = (MdExtensionSet)XamlReader.Parse(
                "<e:MdExtensionSet xmlns:e='clr-namespace:MarkdigWpfViewer.Extensions;assembly=Markdig.Wpf.Viewer'>"
                + "<e:MdExtensionSet /><e:MdExtensionSet /></e:MdExtensionSet>");
            parsed.Extensions.Should().HaveCount(2);
        });
    }

    /// <summary><c>::текст::</c> — оформленный текст «Chip» с обрамлением <c>::</c>.</summary>
    private static LambdaExtension Chips() => new()
    {
        Inlines = (inline, context) => inline is CustomContainerInline container
            ? context.ConvertInlines(container)
                .Select(converted => converted is RunVm run ? run with { StyleName = "Chip", Markup = new("::", "::") } : converted)
                .ToList()
            : null
    };

    private static IEnumerable<Span> Spans(InlineCollection inlines)
        => inlines.OfType<Span>().SelectMany(span => Spans(span.Inlines).Prepend(span));
}
