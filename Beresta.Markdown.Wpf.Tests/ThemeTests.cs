namespace Beresta.Markdown.Wpf.Tests;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Abstractions;
using Presenters;
using Styling;

/// <summary>Вид меняется снаружи: шаблоны, стили начертаний, ключи темы, умолчания от приложения.</summary>
public sealed class ThemeTests
{
    [Fact]
    public void Свой_неявный_стиль_вьюера_без_BasedOn_не_гасит_тему()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "# H\n\ntext" };
            var host = new Border { Child = viewer };
            host.Resources.Add(typeof(MarkdownViewer), new Style(typeof(MarkdownViewer)) { Setters = { new Setter(FrameworkElement.MarginProperty, new Thickness(4)) } });
            var window = WpfHost.Show(host);

            viewer.Margin.Should().Be(new Thickness(4));
            viewer.StyleProvider.Should().NotBeNull();
            Visuals.Descendants<MdTextBlock>(viewer).Should().HaveCount(2);
            window.Close();
        });
    }

    [Fact]
    public void Провайдер_с_BasedOn_заменяет_только_свой_шаблон()
    {
        WpfHost.Invoke(() =>
        {
            var provider = new MdStyleProvider
            {
                BasedOn = (IMdStyleProvider)Application.Current.FindResource("Md.DefaultStyleProvider"),
                HeadingTemplate = TestWidgets.Template("<Border Tag='heading' />")
            };
            var viewer = new MarkdownViewer { StyleProvider = provider, Markdown = "# H\n\ntext" };
            var window = WpfHost.Show(viewer);

            Visuals.Descendants<Border>(viewer).Should().Contain(border => Equals(border.Tag, "heading"));
            Visuals.Descendants<MdHeadingBlock>(viewer).Should().BeEmpty();
            Visuals.Descendants<MdTextBlock>(viewer).Should().ContainSingle();
            window.Close();
        });
    }

    [Fact]
    public void Подчёркивание_сокращения_берёт_кисть_по_ключу_темы_и_следует_её_смене()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "*[HTML]: Hyper Text\n\nHTML here" };
            viewer.Resources["Md.Abbreviation.UnderlineBrush"] = Brushes.Red;
            var window = WpfHost.Show(viewer);
            Underline(viewer).Should().Be(Brushes.Red);

            viewer.Resources["Md.Abbreviation.UnderlineBrush"] = Brushes.Blue;
            Underline(viewer).Should().Be(Brushes.Blue);
            window.Close();
        });

        static Brush Underline(MarkdownViewer viewer)
            => Spans(Visuals.Descendants<MdTextBlock>(viewer).Single().Inlines)
                .Single(span => Equals(span.ToolTip, "Hyper Text"))
                .TextDecorations.Single().Pen.Brush;
    }

    [Fact]
    public void Стиль_начертания_перекрывается_ключом_темы()
    {
        WpfHost.Invoke(() =>
        {
            var strong = new Style(typeof(Span));
            strong.Setters.Add(new Setter(TextElement.ForegroundProperty, Brushes.Red));
            var viewer = new MarkdownViewer { Markdown = "a **b**" };
            viewer.Resources["Md.Strong.Style"] = strong;
            var window = WpfHost.Show(viewer);

            var span = Visuals.Descendants<MdTextBlock>(viewer).Single().Inlines.OfType<Span>().Single();
            span.Foreground.Should().Be(Brushes.Red);
            window.Close();
        });
    }

    [Fact]
    public void Шрифт_и_цвет_текста_берутся_у_приложения()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "# H\n\ntext\n\n- item" };
            var host = new Border { Child = viewer };
            TextElement.SetForeground(host, Brushes.Green);
            TextElement.SetFontFamily(host, new("Consolas"));
            var window = WpfHost.Show(host);

            var texts = Visuals.Descendants<TextBlock>(viewer).ToList();
            texts.Should().HaveCountGreaterThanOrEqualTo(4);
            texts.Should().AllSatisfy(text =>
            {
                text.Foreground.Should().Be(Brushes.Green);
                text.FontFamily.Source.Should().Be("Consolas");
            });
            window.Close();
        });
    }

    [Fact]
    public void Ключ_темы_для_цвета_текста_перекрывает_цвет_приложения()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "text" };
            viewer.Resources["Md.Paragraph.Foreground"] = Brushes.Blue;
            var host = new Border { Child = viewer };
            TextElement.SetForeground(host, Brushes.Green);
            var window = WpfHost.Show(host);

            Visuals.Descendants<MdTextBlock>(viewer).Single().Foreground.Should().Be(Brushes.Blue);
            window.Close();
        });
    }

    private static IEnumerable<Span> Spans(InlineCollection inlines)
        => inlines.OfType<Span>().SelectMany(span => Spans(span.Inlines).Prepend(span));
}