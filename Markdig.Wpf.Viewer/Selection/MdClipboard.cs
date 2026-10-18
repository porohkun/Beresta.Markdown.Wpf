namespace MarkdigWpfViewer.Selection;

using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using Markdig;
using Markdig.Renderers;

/// <summary>Запись в буфер обмена: данные собираются в форматы, проходят <see cref="DataObject.CopyingEvent"/> и кладутся разом.</summary>
internal static class MdClipboard
{
    private const string Header = "Version:0.9\r\nStartHTML:{0:D10}\r\nEndHTML:{1:D10}\r\nStartFragment:{2:D10}\r\nEndFragment:{3:D10}\r\n";
    private const string Prefix = "<html><body>\r\n<!--StartFragment-->";
    private const string Suffix = "<!--EndFragment-->\r\n</body>\r\n</html>";

    /// <summary>Данные из трёх видов: обычный текст, разметка и HTML.</summary>
    public static DataObject Data(string text, string markdown, string html)
    {
        var data = new DataObject();
        data.SetText(text);
        data.SetData(MdDataFormats.Markdown, new MemoryStream(Encoding.UTF8.GetBytes(markdown)));
        data.SetData(DataFormats.Html, HtmlFormat(html));
        return data;
    }

    /// <summary>HTML разметки конвейером разбора; узел без своего HTML-рендерера — исходным текстом.</summary>
    public static string Html(string markdown, MarkdownPipeline pipeline)
    {
        var document = Markdown.Parse(markdown, pipeline);
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

    /// <summary>HTML в обёртке CF_HTML: заголовок с позициями документа и фрагмента в байтах UTF-8.</summary>
    private static string HtmlFormat(string fragment)
    {
        var startHtml = Encoding.UTF8.GetByteCount(string.Format(Header, 0, 0, 0, 0));
        var startFragment = startHtml + Encoding.UTF8.GetByteCount(Prefix);
        var endFragment = startFragment + Encoding.UTF8.GetByteCount(fragment);
        var endHtml = endFragment + Encoding.UTF8.GetByteCount(Suffix);
        return string.Format(Header, startHtml, endHtml, startFragment, endFragment) + Prefix + fragment + Suffix;
    }
}