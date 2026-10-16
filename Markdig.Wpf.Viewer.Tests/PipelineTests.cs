namespace MarkdigWpfViewer.Tests;

using Markdig;

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
}