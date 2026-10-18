namespace Beresta.Markdown.Wpf.Scrolling;

using System.Windows;
using System.Windows.Controls;

/// <summary>
/// Source lines in a <see cref="TextBox"/> by the positions of characters, so wrapping does not get
/// in the way: where a line stands, which one is visible, scrolling to it. A side of scrolling
/// together next to a viewer.
/// </summary>
public sealed class MdTextBoxLines : IMdScrollSide
{
    private string? _text;
    private int[] _starts = [0];

    /// <summary>Source lines of an editor.</summary>
    public MdTextBoxLines(TextBox textBox)
    {
        TextBox = textBox;
    }

    /// <summary>The editor.</summary>
    public TextBox TextBox { get; }

    /// <summary>The scrolling inside the editor.</summary>
    public ScrollViewer? Scroller
    {
        get
        {
            TextBox.ApplyTemplate();
            return TextBox.Template?.FindName("PART_ContentHost", TextBox) as ScrollViewer;
        }
    }

    /// <summary>
    /// The top of a source line in the coordinates of the editor; the fraction is a part of the
    /// line.
    /// </summary>
    public double? GetSourceLineOffset(double line)
    {
        var starts = Starts();
        var whole = (int)Math.Floor(Math.Clamp(line, 0, starts.Length - 1));
        if (Top(whole) is not { } top)
        {
            return null;
        }

        var part = Math.Clamp(line, 0, starts.Length) - whole;
        return part > 0 && Top(whole + 1) is { } next ? top + part * (next - top) : top;
    }

    /// <inheritdoc/>
    public double? GetVisibleSourceLine(double viewportFraction = 0)
    {
        if (Scroller is not { } scroller)
        {
            return null;
        }

        var point = scroller.TranslatePoint(new(1, viewportFraction * scroller.ViewportHeight), TextBox);
        var index = TextBox.GetCharacterIndexFromPoint(point, snapToText: true);
        if (index < 0)
        {
            return null;
        }

        var line = LineOf(index);
        if (Top(line) is not { } top || Top(line + 1) is not { } next || next <= top)
        {
            return line;
        }

        return line + Math.Clamp((point.Y - top) / (next - top), 0, 1);
    }

    /// <inheritdoc/>
    public bool ScrollToSourceLine(double line, double viewportFraction = 0)
    {
        if (Scroller is not { } scroller || GetSourceLineOffset(line) is not { } offset)
        {
            return false;
        }

        var at = TextBox.TranslatePoint(new(0, offset), scroller).Y;
        scroller.ScrollToVerticalOffset(scroller.VerticalOffset + at - viewportFraction * scroller.ViewportHeight);
        return true;
    }

    /// <summary>
    /// Source lines that start in the visible part of the editor: the top and height of the screen
    /// row each one starts with, in the coordinates of an element.
    /// </summary>
    public IReadOnlyList<(int Line, double Top, double Height)> VisibleRows(UIElement relativeTo)
    {
        var rows = new List<(int Line, double Top, double Height)>();
        var first = TextBox.GetFirstVisibleLineIndex();
        var last = TextBox.GetLastVisibleLineIndex();

        // BYDESIGN: видимые строки помнят прежнюю раскладку, пока новый текст не разложен; таких строк уже нет.
        if (first < 0 || last < first || last >= TextBox.LineCount || Scroller is not { } scroller)
        {
            return rows;
        }

        var starts = Starts();
        var from = LineOf(TextBox.GetCharacterIndexFromLineIndex(first));
        var to = LineOf(TextBox.GetCharacterIndexFromLineIndex(last));
        for (var line = from; line <= to; line++)
        {
            var rect = TextBox.GetRectFromCharacterIndex(Math.Min(starts[line], TextBox.Text.Length));
            var top = TextBox.TranslatePoint(rect.TopLeft, scroller).Y;
            if (!rect.IsEmpty && top + rect.Height > 0 && top < scroller.ViewportHeight)
            {
                rows.Add((line, TextBox.TranslatePoint(rect.TopLeft, relativeTo).Y, rect.Height));
            }
        }

        return rows;
    }

    /// <summary>Верх строки в координатах редактора; за последней строкой — низ текста.</summary>
    private double? Top(int line)
    {
        var starts = Starts();
        if (line >= starts.Length)
        {
            var end = TextBox.GetRectFromCharacterIndex(TextBox.Text.Length, trailingEdge: true);
            return end.IsEmpty ? null : end.Bottom;
        }

        var rect = TextBox.GetRectFromCharacterIndex(Math.Min(starts[line], TextBox.Text.Length));
        return rect.IsEmpty ? null : rect.Top;
    }

    /// <summary>Строка исходника знака.</summary>
    private int LineOf(int index)
    {
        var starts = Starts();
        var found = Array.BinarySearch(starts, index);
        return found >= 0 ? found : ~found - 1;
    }

    /// <summary>Начала строк исходника — позиции знаков после переводов строк.</summary>
    private int[] Starts()
    {
        var text = TextBox.Text;
        if (!ReferenceEquals(text, _text))
        {
            _text = text;
            _starts = [0, .. text.Select((symbol, index) => (symbol, index)).Where(pair => pair.symbol == '\n').Select(pair => pair.index + 1)];
        }

        return _starts;
    }
}
