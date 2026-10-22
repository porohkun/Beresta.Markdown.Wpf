namespace Beresta.Markdown.Wpf.Tests;

using Editor;

/// <summary>Поля строк блока исходника: блок высотой нарисованного, строка у своего места, строка без места — в полях соседних.</summary>
public sealed class SourceFitTests
{
    [Fact]
    public void Строки_встают_к_своим_местам_и_добирают_высоту_блока()
    {
        var fits = SourceFit.Fit([(10, 5), (10, 20)], 40);

        fits.Should().Equal((5, 0), (5, 10));
    }

    [Fact]
    public void Строка_без_места_в_конце_поднимает_строки_над_собой()
    {
        var fits = SourceFit.Fit([(18, 13), (16, 37), (18, null)], 66);

        fits.Should().Equal((13, 0), (1, 0), (0, 0));
    }

    [Fact]
    public void Строка_без_места_в_середине_сдвигает_следующие()
    {
        var fits = SourceFit.Fit([(18, 6), (18, null), (18, 34), (18, 63)], 88);

        fits.Should().Equal((6, 0), (0, 0), (0, 0), (3, 7));
    }

    [Fact]
    public void Тесный_блок_встаёт_без_полей()
    {
        var fits = SourceFit.Fit([(20, 5), (20, null)], 30);

        fits.Should().Equal((0, 0), (0, 0));
    }
}
