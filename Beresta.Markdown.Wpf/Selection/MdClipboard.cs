namespace Beresta.Markdown.Wpf.Selection;

using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

/// <summary>Запись в буфер обмена: данные собираются в форматы, проходят <see cref="DataObject.CopyingEvent"/> и кладутся разом.</summary>
internal static class MdClipboard
{
    private const string Header = "Version:0.9\r\nStartHTML:{0:D10}\r\nEndHTML:{1:D10}\r\nStartFragment:{2:D10}\r\nEndFragment:{3:D10}\r\n";
    private const string Prefix = "<html><body>\r\n<!--StartFragment-->";
    private const string Suffix = "<!--EndFragment-->\r\n</body>\r\n</html>";

    /// <summary>
    /// Данные из трёх видов: обычный текст, разметка и HTML; без HTML — из двух. Абсолютный
    /// <paramref name="source"/> — адрес документа, от которого принимающая программа разбирает
    /// относительные ссылки HTML.
    /// </summary>
    public static DataObject Data(string text, string markdown, string? html, Uri? source = null)
    {
        var data = new DataObject();
        data.SetText(text);
        data.SetData(MdDataFormats.Markdown, new MemoryStream(Encoding.UTF8.GetBytes(markdown)));
        if (html is not null)
        {
            data.SetData(DataFormats.Html, HtmlFormat(html, source));
        }

        return data;
    }

    /// <summary>
    /// HTML разметки конвейером разбора; узел без своего HTML-рендерера — исходным текстом. Картинка,
    /// которую <paramref name="shows"/> не пускает на экран, идёт своим alt-текстом, остальные — с
    /// абсолютным адресом, если он разобрался от <paramref name="baseUri"/>.
    /// </summary>
    public static string Html(string markdown, MarkdownPipeline pipeline, Uri? baseUri, Func<string, Uri?, bool> shows)
    {
        var document = Markdown.Parse(markdown, pipeline);
        foreach (var image in ImagesOf(document))
        {
            var url = image.Url ?? string.Empty;
            var resolved = ImageAddress.Resolve(url, baseUri);
            if (!shows(url, resolved))
            {
                image.ReplaceBy(new LiteralInline(ModelBuilder.PlainText(image.FirstChild)), copyChildren: false);
            }
            else if (resolved is { IsAbsoluteUri: true })
            {
                image.Url = resolved.AbsoluteUri;
            }
        }

        using var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer);
        pipeline.Setup(renderer);
        renderer.ObjectRenderers.Add(new SourceHtmlRenderer(markdown));
        renderer.Render(document);
        writer.Flush();
        return writer.ToString();
    }

    /// <summary>Поднимает <see cref="DataObject.CopyingEvent"/> на источнике; <c>false</c> — приложение отменило копирование.</summary>
    public static bool Approve(UIElement source, DataObject data)
    {
        var copying = new DataObjectCopyingEventArgs(data, false);
        source.RaiseEvent(copying);
        return !copying.CommandCancelled;
    }

    public static void Put(DataObject data)
    {
        try
        {
            Clipboard.SetDataObject(data, true);
        }
        catch (COMException)
        {
            // BYDESIGN: буфер обмена бывает занят другим приложением — копирование тогда просто
            // не удаётся, а вьюер работает дальше.
        }
    }

    /// <summary>Картинки документа — обходом дерева без рекурсии.</summary>
    private static List<LinkInline> ImagesOf(MarkdownDocument document)
    {
        var images = new List<LinkInline>();
        var next = new Stack<MarkdownObject>([document]);
        while (next.TryPop(out var node))
        {
            if (node is LinkInline { IsImage: true } image)
            {
                images.Add(image);
            }

            IEnumerable<MarkdownObject> children = node switch
            {
                ContainerBlock container => container,
                LeafBlock { Inline: { } inline } => [inline],
                ContainerInline container => container,
                _ => []
            };

            foreach (var child in children)
            {
                next.Push(child);
            }
        }

        return images;
    }

    /// <summary>
    /// HTML в обёртке CF_HTML: заголовок с позициями документа и фрагмента в байтах UTF-8 и, если
    /// <paramref name="source"/> абсолютный, с адресом документа.
    /// </summary>
    private static string HtmlFormat(string fragment, Uri? source)
    {
        var sourceLine = source is { IsAbsoluteUri: true } ? $"SourceURL:{source.AbsoluteUri}\r\n" : string.Empty;
        var startHtml = Encoding.UTF8.GetByteCount(string.Format(Header, 0, 0, 0, 0) + sourceLine);
        var startFragment = startHtml + Encoding.UTF8.GetByteCount(Prefix);
        var endFragment = startFragment + Encoding.UTF8.GetByteCount(fragment);
        var endHtml = endFragment + Encoding.UTF8.GetByteCount(Suffix);
        return string.Format(Header, startHtml, endHtml, startFragment, endFragment) + sourceLine + Prefix + fragment + Suffix;
    }
}
