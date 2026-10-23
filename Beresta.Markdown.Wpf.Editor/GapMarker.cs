namespace Beresta.Markdown.Wpf.Editor;

using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using ICSharpCode.AvalonEdit.Rendering;

/// <summary>
/// Знаки цитаты пустой строки цитаты: рисуются тем же шрифтом, что и текст, но высоты строке не дают — её
/// задаёт распорка отступа. AvalonEdit считает их пробелами.
/// </summary>
internal sealed class GapMarker(int length, string text) : VisualLineElement(length, length)
{
    /// <inheritdoc/>
    public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
        => new GapMarkerRun(text, TextRunProperties, TextOptions.GetTextFormattingMode(context.TextView), VisualTreeHelper.GetDpi(context.TextView).PixelsPerDip);

    /// <inheritdoc/>
    public override bool IsWhitespace(int visualColumn) => true;
}
