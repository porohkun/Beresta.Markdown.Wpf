namespace MarkdigWpfViewer.Tests;

using Kit;
using Markdig;
using ViewModels;

/// <summary>Настройка разбора, переданная снаружи.</summary>
public sealed class PipelineTests
{
    private const string Table = "| a | b |\n|---|---|\n| 1 | 2 |";

    [Fact]
    public void По_умолчанию_расширения_включены()
    {
        ModelDump.Of(Table).Should().Be("T(2x2)");
    }

    [Fact]
    public void Переданная_настройка_заменяет_умолчание()
    {
        ModelDump.Of(Table, new MarkdownPipelineBuilder().Build()).Should().StartWith("P(");
    }

    [Fact]
    public void Настройка_построителя_работает_и_с_расширениями()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer
            {
                PipelineSetup = builder => builder.UseAdvancedExtensions().DisableHtml(),
                Extensions = { new Tags() },
                Markdown = "<b>bold</b> text #tag"
            };
            var window = WpfHost.Show(viewer);

            viewer.Items.OfType<ParagraphVm>().Single().Inlines.OfType<RunVm>().Should().Contain(run => run.StyleName == "Tag");
            viewer.TextMap.Text.Should().StartWith("<b>bold</b> text");
            window.Close();
        });
    }

    [Fact]
    public void Настройка_построителя_главнее_готового_конвейера()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer
            {
                MarkdownPipeline = new MarkdownPipelineBuilder().Build(),
                PipelineSetup = builder => builder.UsePipeTables(),
                Markdown = Table
            };
            var window = WpfHost.Show(viewer);

            ModelDump.Of(viewer.Items.OfType<BlockVm>()).Should().Be("T(2x2)");
            window.Close();
        });
    }
}