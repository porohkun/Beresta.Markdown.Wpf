namespace Beresta.Markdown.Wpf.Tests;

using Editor;
using ICSharpCode.AvalonEdit.Document;

/// <summary>Версии текста: место из показанного текста переводится на нынешний документ, сколько бы текстов ни прошло.</summary>
public sealed class SourceVersionsTests
{
    [Fact]
    public void Давний_текст_переводится_и_после_многих_новых()
    {
        var document = new TextDocument("one");
        var versions = new SourceVersions();
        var shown = document.Text;
        versions.Remember(shown, document.Version);

        for (var i = 0; i < 100; i++)
        {
            document.Insert(0, "x");
            versions.Remember(document.Text, document.Version);
        }

        versions.Map(shown, 3, document, AnchorMovementType.Default).Should().Be(103);
        versions.ToText(shown, 103, document, AnchorMovementType.Default).Should().Be(3);
    }
}
