namespace Beresta.Markdown.Wpf.Editor;

using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

/// <summary>Совпадения поиска по исходнику фоном под текстом AvalonEdit: кистями пометок <c>Found</c> и <c>Current</c>.</summary>
internal sealed class SearchBackground(MdSourceSearch search) : IBackgroundRenderer
{
    /// <inheritdoc/>
    public KnownLayer Layer => KnownLayer.Selection;

    /// <inheritdoc/>
    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (!textView.VisualLinesValid || textView.VisualLines.Count == 0 || search.Count == 0)
        {
            return;
        }

        var found = textView.TryFindResource("Md.Mark.Found.Brush") as Brush;
        var current = textView.TryFindResource("Md.Mark.Current.Brush") as Brush;
        var first = textView.VisualLines[0].FirstDocumentLine.Offset;
        var last = textView.VisualLines[^1].LastDocumentLine.EndOffset;
        for (var index = 0; index < search.Matches.Count; index++)
        {
            var match = search.Matches[index];
            if (match.End < first || match.Start > last || (index == search.CurrentIndex ? current : found) is not { } brush)
            {
                continue;
            }

            foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, new TextSegment { StartOffset = match.Start, Length = match.Length }))
            {
                drawingContext.DrawRectangle(brush, null, rect);
            }
        }
    }
}
