namespace MarkdigWpfViewer.Presenters;

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ViewModels;

/// <summary>
/// Поля с номерами строк исходника: у блока — номер первой строки, у блока кода — каждой.
/// Ширина общая на документ — по числу цифр последней строки.
/// </summary>
public sealed class MdLineGutter : Control
{
    static MdLineGutter()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(MdLineGutter), new FrameworkPropertyMetadata(typeof(MdLineGutter)));
    }

    /// <summary>Номера созданных блоков: строка исходника, верх и высота её текста в координатах полей.</summary>
    internal IReadOnlyList<(int Line, double Top, double Height)> CollectNumbers()
    {
        if (TemplatedParent is not MarkdownViewer { BlockPanel: { } panel })
        {
            return [];
        }

        var numbers = new SortedDictionary<int, Place>();
        var pending = new Stack<DependencyObject>();
        pending.Push(panel);

        while (pending.Count > 0)
        {
            var node = pending.Pop();
            var content = node switch
            {
                ContentPresenter presenter => presenter.Content,
                ContentControl control => control.Content,
                _ => null
            };

            if (node is FrameworkElement { ActualHeight: > 0 } element
                && content is NodeVm { Source.IsEmpty: false } model and not (BlankLineVm or InlineVm))
            {
                if (model is CodeBlockVm code && First<CodeBlockPresenter>(element) is { } presenter &&
                    First<MdTextBlock>(presenter) is { ActualHeight: > 0 } text)
                {
                    RememberCodeLines(numbers, code, text);
                }
                else
                {
                    Remember(numbers, model.Source.StartLine, PlaceOf(element, model.Source.StartLine));
                }
            }

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            {
                pending.Push(VisualTreeHelper.GetChild(node, i));
            }
        }

        return WithoutOverlaps(numbers);
    }

    protected override Size MeasureOverride(Size constraint)
    {
        var digits = Math.Max(2, (LastLine() + 1).ToString(CultureInfo.InvariantCulture).Length);
        var width = Format(new('9', digits)).WidthIncludingTrailingWhitespace;
        return new(Math.Ceiling(width) + Padding.Left + Padding.Right, 0);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        foreach (var (line, top, height) in CollectNumbers())
        {
            var text = Format((line + 1).ToString(CultureInfo.InvariantCulture));
            var x = ActualWidth - Padding.Right - text.WidthIncludingTrailingWhitespace;
            var y = top + (height - text.Height) / 2;
            drawingContext.DrawText(text, new(x, y));
        }
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == FontFamilyProperty
            || e.Property == FontSizeProperty
            || e.Property == FontStyleProperty
            || e.Property == FontWeightProperty
            || e.Property == FontStretchProperty
            || e.Property == PaddingProperty)
        {
            InvalidateMeasure();
            InvalidateVisual();
        }
        else if (e.Property == ForegroundProperty)
        {
            InvalidateVisual();
        }
    }

    private static T? First<T>(DependencyObject root)
        where T : DependencyObject
    {
        if (root is T self)
        {
            return self;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            if (First<T>(VisualTreeHelper.GetChild(root, i)) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>Из номеров, налезающих друг на друга, оставляет один: на строке текста — охотнее, чем у рамки.</summary>
    private static List<(int Line, double Top, double Height)> WithoutOverlaps(SortedDictionary<int, Place> numbers)
    {
        var placed = new List<(int Line, Place Place)>();

        foreach (var (line, place) in numbers.OrderBy(pair => pair.Value.Top))
        {
            if (placed.Count > 0 && placed[^1].Place is var last && last.Top + last.Height > place.Top + 0.5)
            {
                if (place.OnText && !last.OnText)
                {
                    placed[^1] = (line, place);
                }

                continue;
            }

            placed.Add((line, place));
        }

        return placed.OrderBy(item => item.Line).Select(item => (item.Line, item.Place.Top, item.Place.Height)).ToList();
    }

    /// <summary>Строку текста предпочитает рамке блока, из равных — верхнюю.</summary>
    private static void Remember(SortedDictionary<int, Place> numbers, int line, Place place)
    {
        if (!numbers.TryGetValue(line, out var known)
            || (place.OnText != known.OnText ? place.OnText : place.Top < known.Top))
        {
            numbers[line] = place;
        }
    }

    private void RememberCodeLines(SortedDictionary<int, Place> numbers, CodeBlockVm code, MdTextBlock text)
    {
        var lines = code.Code.Split('\n').Length;
        var top = text.TranslatePoint(default, this).Y;
        var lineHeight = text.ActualHeight / lines;

        for (var i = 0; i < lines; i++)
        {
            Remember(numbers, code.CodeFirstLine + i, new(top + i * lineHeight, lineHeight, OnText: true));
        }
    }

    /// <summary>Первая строка текста блока, если текст начинается на первой строке его исходника; иначе — верх рамки.</summary>
    private Place PlaceOf(FrameworkElement element, int line)
    {
        if (First<MdTextBlock>(element) is { ActualHeight: > 0 } text
            && text.Runs?.FirstOrDefault() is { Source: { IsEmpty: false } source }
            && source.StartLine == line
            && text.GetRangeRects(0, 1).FirstOrDefault() is { Height: > 0 } rect)
        {
            return new(text.TranslatePoint(rect.TopLeft, this).Y, rect.Height, OnText: true);
        }

        return new(element.TranslatePoint(default, this).Y, Format("0").Height, OnText: false);
    }

    private int LastLine()
    {
        return TemplatedParent is MarkdownViewer { ItemsSource: IReadOnlyList<BlockVm> { Count: > 0 } items }
            ? items.Max(item => item.Source.EndLine)
            : 0;
    }

    private FormattedText Format(string text)
    {
        return new(
            text,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new(FontFamily, FontStyle, FontWeight, FontStretch),
            FontSize,
            Foreground ?? Brushes.Gray,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
    }

    /// <summary>Где стоит номер: полоса по вертикали и стоит ли она на строке текста.</summary>
    private readonly record struct Place(double Top, double Height, bool OnText);
}