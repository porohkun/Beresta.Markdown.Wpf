namespace Beresta.Markdown.Wpf.Tests;

/// <summary>Тесты, которые меняют общее состояние картинок — кэш и пределы загрузчика: идут одни, без параллельных соседей.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SharedImageState
{
    public const string Name = "Shared image state";
}
