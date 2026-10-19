namespace Beresta.Markdown.Wpf.Tests;

using System.Reflection;
using System.Windows.Controls;
using System.Windows.Markup;
using Behaviors;
using Kit;

/// <summary>Все публичные пространства имён библиотеки — под одним XML-пространством XAML.</summary>
public sealed class XamlNamespaceTests
{
    private const string Uri = "https://github.com/porohkun/Beresta.Markdown.Wpf";

    [Fact]
    public void Одно_пространство_XAML_отображает_все_публичные_типы_без_повторов_имён()
    {
        var assembly = typeof(MarkdownViewer).Assembly;
        var mapped = assembly
            .GetCustomAttributes<XmlnsDefinitionAttribute>()
            .Where(attribute => attribute.XmlNamespace == Uri)
            .Select(attribute => attribute.ClrNamespace)
            .ToHashSet();
        var types = assembly
            .GetExportedTypes()
            .Where(type => type.Namespace!.StartsWith("Beresta.Markdown.Wpf", StringComparison.Ordinal))
            .ToList();

        types.Select(type => type.Namespace!).Should().OnlyContain(name => mapped.Contains(name));
        types.Select(type => type.Name).Should().OnlyHaveUniqueItems();
        assembly.GetCustomAttribute<XmlnsPrefixAttribute>()!.Prefix.Should().Be("md");
    }

    [Fact]
    public void Разметка_с_одним_пространством_разбирается()
    {
        WpfHost.Invoke(() =>
        {
            var scroll = (ScrollViewer)XamlReader.Parse(
                $"""
                <ScrollViewer xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                              xmlns:md="{Uri}"
                              md:StickyScroll.Edge="Bottom">
                    <md:MarkdownViewer>
                        <md:MarkdownViewer.Extensions>
                            <md:ObsidianSet />
                        </md:MarkdownViewer.Extensions>
                    </md:MarkdownViewer>
                </ScrollViewer>
                """);

            StickyScroll.GetEdge(scroll).Should().Be(StickyEdge.Bottom);
            scroll.Content.Should().BeOfType<MarkdownViewer>().Which.Extensions.Should().ContainSingle().Which.Should().BeOfType<ObsidianSet>();
        });
    }
}
