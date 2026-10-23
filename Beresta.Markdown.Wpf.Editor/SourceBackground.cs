namespace Beresta.Markdown.Wpf.Editor;

using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Rendering;

/// <summary>Фон и полоса слева у строк с видом — блок кода, цитата; виды строки документа даёт редактор.</summary>
internal sealed class SourceBackground(Func<int, IEnumerable<LookStyle>> looksAt) : IBackgroundRenderer
{
    /// <inheritdoc/>
    public KnownLayer Layer => KnownLayer.Background;

    /// <inheritdoc/>
    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (!textView.VisualLinesValid)
        {
            return;
        }

        foreach (var line in textView.VisualLines)
        {
            var top = line.VisualTop - textView.ScrollOffset.Y;
            foreach (var style in looksAt(line.FirstDocumentLine.LineNumber))
            {
                if (style.Background is { } fill)
                {
                    drawingContext.DrawRectangle(fill, null, new Rect(0, top, textView.ActualWidth, line.Height));
                }

                if (style.Bar is { } bar)
                {
                    drawingContext.DrawRectangle(bar, null, new Rect(0, top, Math.Max(1, style.BarWidth), line.Height));
                }
            }
        }
    }
}
