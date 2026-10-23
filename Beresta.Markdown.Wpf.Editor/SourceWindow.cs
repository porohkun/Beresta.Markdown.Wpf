namespace Beresta.Markdown.Wpf.Editor;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using Scrolling;

/// <summary>
/// Окно правки: AvalonEdit на холсте, который вьюер ставит вместо правимых блоков. Края окна — якоря в
/// документе; высота холста — высота строк окна, AvalonEdit — высотой с видимую часть холста и запасом,
/// прокручен изнутри к своим строкам. Строки для вьюера — в номерах показанного текста и по тексту, без полей распорок.
/// </summary>
internal sealed class SourceWindow : IMdSourceRows
{
    /// <summary>Окно до стольких строк домеряется целиком: его высота точная.</summary>
    private const int MeasureLimit = 400;

    /// <summary>Запас AvalonEdit за краями видимой части холста.</summary>
    private const double Overscan = 150;

    private readonly TextEditor _editor;
    private readonly Func<int, int> _toShown;
    private readonly Func<int, int> _toDocument;
    private readonly Func<int, (double Above, double Below)> _margins;
    private TextAnchor? _start;
    private TextAnchor? _end;

    /// <param name="editor">AvalonEdit редактора.</param>
    /// <param name="toShown">Строка документа с единицы — строка показанного текста с нуля.</param>
    /// <param name="toDocument">Строка показанного текста с нуля — строка документа с единицы.</param>
    /// <param name="margins">Поля, которые распорка добавляет над текстом строки документа с единицы и под ним.</param>
    public SourceWindow(TextEditor editor, Func<int, int> toShown, Func<int, int> toDocument, Func<int, (double Above, double Below)> margins)
    {
        _editor = editor;
        _toShown = toShown;
        _toDocument = toDocument;
        _margins = margins;

        // BYDESIGN: высота — с самого начала: на холсте AvalonEdit без высоты раскладывает весь документ
        _editor.Height = 1;
        _editor.TextArea.TextView.VisualLinesChanged += (_, _) => RowsChanged?.Invoke(this, EventArgs.Empty);
        Element = new Canvas { ClipToBounds = true };
        Element.Children.Add(editor);
        Element.SizeChanged += (_, _) =>
        {
            if (Element.ActualWidth > 0)
            {
                _editor.Width = Element.ActualWidth;
            }
        };
    }

    /// <inheritdoc/>
    public event EventHandler? RowsChanged;

    /// <summary>Холст окна: элемент, который вьюер ставит вместо блоков.</summary>
    public Canvas Element { get; }

    public bool IsOpen => _start is not null && _end is not null;

    /// <summary>Окно само ставит внутреннюю прокрутку AvalonEdit — это не самопрокрутка.</summary>
    public bool IsPlacing { get; private set; }

    /// <summary>Строки окна в документе, с единицы.</summary>
    public (int First, int Last) Lines
    {
        get
        {
            var document = _editor.Document;
            var first = document.GetLineByOffset(Math.Min(_start!.Offset, document.TextLength)).LineNumber;
            var last = document.GetLineByOffset(Math.Min(_end!.Offset, document.TextLength)).LineNumber;
            return (first, Math.Max(first, last));
        }
    }

    /// <summary>Высота строк окна — сколько места оно занимает во вьюере.</summary>
    public double Height => Math.Max(1, LineBottom(Lines.Last) - LineTop(Lines.First));

    /// <summary>
    /// Ставит окно на строки документа с единицы: начало держится перед вставкой, конец — после, оба
    /// переживают удаление.
    /// </summary>
    public void Open(int first, int last)
    {
        var document = _editor.Document;
        var from = document.GetLineByNumber(Math.Clamp(first, 1, document.LineCount));
        var to = document.GetLineByNumber(Math.Clamp(last, from.LineNumber, document.LineCount));
        _start = document.CreateAnchor(from.Offset);
        _start.MovementType = AnchorMovementType.BeforeInsertion;
        _start.SurviveDeletion = true;
        _end = document.CreateAnchor(to.EndOffset);
        _end.MovementType = AnchorMovementType.AfterInsertion;
        _end.SurviveDeletion = true;
    }

    public void Close()
    {
        _start = null;
        _end = null;
    }

    /// <summary>Верх строки документа в раскладке AvalonEdit.</summary>
    public double LineTop(int line) => _editor.TextArea.TextView.GetVisualTopByDocumentLine(Math.Clamp(line, 1, _editor.Document.LineCount));

    /// <summary>Низ строки документа — верх следующей; у последней строки — высота всего документа.</summary>
    public double LineBottom(int line)
        => line < _editor.Document.LineCount ? LineTop(line + 1) : _editor.TextArea.TextView.DocumentHeight;

    /// <summary>Верх текста строки документа в раскладке AvalonEdit — под полем, которое добавила распорка.</summary>
    public double TextTop(int line) => LineTop(line) + _margins(Math.Clamp(line, 1, _editor.Document.LineCount)).Above;

    /// <summary>Высота текста строки документа — без полей распорки.</summary>
    private double TextHeight(int line)
    {
        var (above, below) = _margins(Math.Clamp(line, 1, _editor.Document.LineCount));
        return Math.Max(0, LineBottom(line) - LineTop(line) - above - below);
    }

    /// <summary>Домеряет строки небольшого окна и ставит холсту высоту строк окна.</summary>
    public void Refit()
    {
        var view = _editor.TextArea.TextView;
        var (first, last) = Lines;
        if (last - first < MeasureLimit && view.ActualWidth > 0)
        {
            for (var line = first; line <= last; line++)
            {
                view.GetOrConstructVisualLine(_editor.Document.GetLineByNumber(line));
            }
        }

        var height = Height;
        if (double.IsNaN(Element.Height) || Math.Abs(Element.Height - height) > 0.5)
        {
            Element.Height = height;
        }
    }

    /// <summary>
    /// Ставит AvalonEdit на видимую часть холста — от <paramref name="visibleTop"/> до
    /// <paramref name="visibleBottom"/> в его координатах — с запасом и прокручивает изнутри к той же
    /// части строк окна. Холст целиком за краем — AvalonEdit в пиксель у его края.
    /// </summary>
    public void Place(double visibleTop, double visibleBottom)
    {
        var height = double.IsNaN(Element.Height) ? Element.ActualHeight : Element.Height;
        var from = Math.Clamp(visibleTop - Overscan, 0, height);
        var to = Math.Clamp(visibleBottom + Overscan, 0, height);
        var size = Math.Max(1, to - from);
        from = Math.Max(0, Math.Min(from, height - size));

        if (double.IsNaN(Canvas.GetTop(_editor)) || Math.Abs(Canvas.GetTop(_editor) - from) > 0.5)
        {
            Canvas.SetTop(_editor, from);
        }

        if (double.IsNaN(_editor.Height) || Math.Abs(_editor.Height - size) > 0.5)
        {
            _editor.Height = size;
        }

        var offset = LineTop(Lines.First) + from;
        var view = _editor.TextArea.TextView;
        if (Math.Abs(view.ScrollOffset.Y - offset) > 0.5)
        {
            IsPlacing = true;
            try
            {
                ((IScrollInfo)view).SetVerticalOffset(offset);
            }
            finally
            {
                IsPlacing = false;
            }
        }
    }

    /// <summary>Верх и высота курсора в координатах холста.</summary>
    public (double Top, double Height) Caret()
    {
        var rect = _editor.TextArea.Caret.CalculateCaretRectangle();
        return (rect.Top - LineTop(Lines.First), rect.Height);
    }

    /// <summary>Место в документе по точке холста; за строками окна — <c>null</c>.</summary>
    public int? PositionAt(Point point)
    {
        var (first, last) = Lines;
        var view = _editor.TextArea.TextView;
        var visual = new Point(Math.Max(0, point.X), LineTop(first) + point.Y);
        if (view.GetPosition(visual) is { } position && position.Line >= first && position.Line <= last)
        {
            return _editor.Document.GetOffset(position.Location);
        }

        var line = view.GetDocumentLineByVisualTop(visual.Y);
        return line.LineNumber >= first && line.LineNumber <= last ? line.Offset : null;
    }

    /// <summary>Строки документа, которые AvalonEdit показывает в своей видимой части, хотя они вне окна.</summary>
    public IReadOnlyList<int> ForeignLines()
    {
        var view = _editor.TextArea.TextView;
        if (!IsOpen || !view.VisualLinesValid)
        {
            return [];
        }

        var (first, last) = Lines;
        var top = view.ScrollOffset.Y;
        var bottom = top + _editor.ActualHeight;
        return view.VisualLines
            .Where(line => line.VisualTop + line.Height > top + 0.5 && line.VisualTop < bottom - 0.5)
            .Where(line => line.FirstDocumentLine.LineNumber < first || line.LastDocumentLine.LineNumber > last)
            .Select(line => line.FirstDocumentLine.LineNumber)
            .ToList();
    }

    /// <inheritdoc/>
    public double? GetSourceLineOffset(double line)
    {
        if (!IsOpen)
        {
            return null;
        }

        var (first, last) = Lines;
        var whole = (int)Math.Floor(line);
        var documentLine = Math.Clamp(_toDocument(whole), first, last + 1);
        var top = (documentLine <= last ? TextTop(documentLine) : LineBottom(last)) - LineTop(first);
        var part = line - whole;
        return part > 0 && documentLine <= last ? top + part * TextHeight(documentLine) : top;
    }

    /// <inheritdoc/>
    public double? GetSourceLineAt(double offset)
    {
        if (!IsOpen)
        {
            return null;
        }

        var (first, last) = Lines;
        var visual = LineTop(first) + Math.Max(0, offset);
        var line = Math.Clamp(_editor.TextArea.TextView.GetDocumentLineByVisualTop(visual).LineNumber, first, last);
        var top = TextTop(line);
        var height = TextHeight(line);
        return _toShown(line) + (height > 0 ? Math.Clamp((visual - top) / height, 0, 1) : 0);
    }

    /// <inheritdoc/>
    public IReadOnlyList<(int Line, double Top, double Height)> VisibleRows()
    {
        var view = _editor.TextArea.TextView;
        if (!IsOpen || !view.VisualLinesValid)
        {
            return [];
        }

        var (first, last) = Lines;
        var origin = LineTop(first);
        return view.VisualLines
            .Where(line => line.FirstDocumentLine.LineNumber >= first && line.FirstDocumentLine.LineNumber <= last)
            .Select(line => (Line: line, Margins: _margins(line.FirstDocumentLine.LineNumber)))
            .Select(entry => (
                _toShown(entry.Line.FirstDocumentLine.LineNumber),
                entry.Line.VisualTop - origin + entry.Margins.Above,
                Math.Max(0, entry.Line.TextLines[0].Height - entry.Margins.Above - (entry.Line.TextLines.Count == 1 ? entry.Margins.Below : 0))))
            .ToList();
    }
}
