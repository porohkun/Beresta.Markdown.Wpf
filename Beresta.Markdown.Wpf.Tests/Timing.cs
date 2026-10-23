namespace Beresta.Markdown.Wpf.Tests;

/// <summary>Тесты, которые ограничивают время сверху: идут одни, без параллельных соседей.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class Timing
{
    public const string Name = "Timing";
}
