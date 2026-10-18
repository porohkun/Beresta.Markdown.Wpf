namespace Beresta.Markdown.Wpf.Presenters;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Scrolling;
using ViewModels;

/// <summary>
/// Таблица сеткой: строка таблицы — строка сетки, а пустое место перед строкой — своя строка сетки.
/// Столбцы укладываются в доступную ширину, как в браузере: не уже самого длинного слова и не шире
/// текста без переносов.
/// </summary>
public sealed class MdTablePresenter : Control
{
    public static readonly DependencyProperty TableProperty =
        DependencyProperty.Register(
            nameof(Table),
            typeof(TableVm),
            typeof(MdTablePresenter),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnAnyPropertyChanged));

    public static readonly DependencyProperty CellTemplateProperty =
        DependencyProperty.Register(
            nameof(CellTemplate),
            typeof(DataTemplate),
            typeof(MdTablePresenter),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnAnyPropertyChanged));

    public static readonly DependencyProperty HeaderTemplateProperty =
        DependencyProperty.Register(
            nameof(HeaderTemplate),
            typeof(DataTemplate),
            typeof(MdTablePresenter),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, OnAnyPropertyChanged));

    private static readonly DependencyProperty GapsProperty =
        GapLayout.LayoutProperty.AddOwner(
            typeof(MdTablePresenter),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, OnAnyPropertyChanged));

    private readonly List<int> _gridRows = [];
    private readonly List<(ContentPresenter Cell, int Column, int Span)> _cells = [];
    private Grid? _grid;
    private (double[] Least, double[] Natural)? _columns;
    private double _measuredWidth = double.NaN;

    public MdTablePresenter()
    {
        Build();
    }

    public TableVm? Table
    {
        get => (TableVm?)GetValue(TableProperty);
        set => SetValue(TableProperty, value);
    }

    public DataTemplate CellTemplate
    {
        get => (DataTemplate)GetValue(CellTemplateProperty);
        set => SetValue(CellTemplateProperty, value);
    }

    public DataTemplate HeaderTemplate
    {
        get => (DataTemplate)GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    /// <summary>
    /// Полосы строк исходника таблицы в своих координатах: строка таблицы — от рамки до рамки, у первой
    /// и последней — от края таблицы; строки исходника между строками таблицы — граница без высоты.
    /// </summary>
    internal IEnumerable<(Rect Band, int Start, int End, TableRowVm? Row)> SourceRows()
    {
        if (Table is not { Source.IsEmpty: false } table || _grid is null || _gridRows.Count != table.Rows.Count)
        {
            yield break;
        }

        var toPresenter = _grid.TransformToAncestor(this);
        var next = table.Source.StartLine;
        var bottom = 0.0;
        for (var r = 0; r < table.Rows.Count; r++)
        {
            var row = table.Rows[r];
            if (row.Source.IsEmpty)
            {
                continue;
            }

            var definition = _grid.RowDefinitions[_gridRows[r]];
            var top = r == 0 ? 0 : toPresenter.Transform(new(0, definition.Offset)).Y;
            if (next < row.Source.StartLine)
            {
                yield return (new(0, bottom, ActualWidth, 0), next, row.Source.StartLine, null);
            }

            bottom = r == table.Rows.Count - 1
                ? ActualHeight
                : toPresenter.Transform(new(0, definition.Offset + definition.ActualHeight)).Y;
            yield return (new(0, top, ActualWidth, Math.Max(0, bottom - top)), row.Source.StartLine, row.Source.EndLine + 1, row);
            next = row.Source.EndLine + 1;
        }

        if (next <= table.Source.EndLine)
        {
            yield return (new(0, bottom, ActualWidth, 0), next, table.Source.EndLine + 1, null);
        }
    }

    protected override Size MeasureOverride(Size constraint)
    {
        if (_grid is not null && Table is { ColumnCount: > 0 })
        {
            // BYDESIGN: та же ширина значит, что поменялось содержимое, — ширины столбцов меряются
            // заново; другая ширина — прежние подходят, меняется только раскладка.
            if (_columns is null || constraint.Width.Equals(_measuredWidth))
            {
                _columns = MeasureColumns();
            }

            _measuredWidth = constraint.Width;
            var (least, natural) = _columns.Value;
            var widths = Distribute(least, natural, constraint.Width - ChromeWidth());
            for (var c = 0; c < widths.Length; c++)
            {
                if (_grid.ColumnDefinitions[c].Width != new GridLength(widths[c]))
                {
                    _grid.ColumnDefinitions[c].Width = new(widths[c]);
                }
            }

            _grid.InvalidateMeasure();
        }

        return base.MeasureOverride(constraint);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _grid = GetTemplateChild("PART_Grid") as Grid;

        Build();
    }

    private static void OnAnyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MdTablePresenter)d).Build();

    /// <summary>
    /// Ширины столбцов в доступной ширине: естественные, если влезают; наименьшие, если не влезают и
    /// они; иначе запас сверх наименьших делится по тому, сколько каждому не хватает до естественной.
    /// </summary>
    private static double[] Distribute(double[] least, double[] natural, double available)
    {
        var leastSum = least.Sum();
        var naturalSum = natural.Sum();
        if (double.IsInfinity(available) || naturalSum <= available)
        {
            return natural;
        }

        // BYDESIGN: полпикселя запаса — сумма дробных ширин не должна перевалить за доступную
        // и включить прокрутку.
        var room = available - 0.5;
        if (leastSum >= room || naturalSum <= leastSum)
        {
            return least;
        }

        var share = (room - leastSum) / (naturalSum - leastSum);
        return least
            .Zip(natural, (low, high) => low + (high - low) * share)
            .ToArray();
    }

    /// <summary>Расширяет столбцы объединённой ячейки поровну, если вместе они уже неё.</summary>
    private static void Widen(double[] widths, int column, int span, double width)
    {
        var lack = width - widths.Skip(column).Take(span).Sum();
        for (var c = column; lack > 0 && c < column + span; c++)
        {
            widths[c] += lack / span;
        }
    }

    /// <summary>Видимые тексты ячейки; внутрь текста и свёрнутого не заходит.</summary>
    private static IEnumerable<MdTextBlock> Texts(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            switch (VisualTreeHelper.GetChild(root, i))
            {
                case UIElement { Visibility: not Visibility.Visible }:
                    continue;
                case MdTextBlock text:
                    yield return text;
                    continue;
                case var child:
                    foreach (var deeper in Texts(child))
                    {
                        yield return deeper;
                    }

                    continue;
            }
        }
    }

    /// <summary>
    /// Наименьшая и естественная ширина каждого столбца. Естественная — ячейка без ограничения ширины,
    /// наименьшая — та же ячейка, где каждый текст сжат до своего самого длинного слова.
    /// </summary>
    private (double[] Least, double[] Natural) MeasureColumns()
    {
        var count = Table!.ColumnCount;
        var least = new double[count];
        var natural = new double[count];
        var spanned = new List<(int Column, int Span, double Least, double Natural)>();

        foreach (var (cell, column, span) in _cells)
        {
            // BYDESIGN: пиксель запаса — округление раскладки срезает желаемую ширину текста и вниз,
            // и текст ровно по ней переносит последнее слово.
            cell.Measure(new(double.PositiveInfinity, double.PositiveInfinity));
            var cellNatural = Math.Ceiling(cell.DesiredSize.Width) + 1;
            var cellLeast = Math.Ceiling(Texts(cell)
                .Select(text => cell.DesiredSize.Width - text.DesiredSize.Width + text.LongestWordWidth())
                .DefaultIfEmpty(cellNatural)
                .Max()) + 1;

            if (span == 1)
            {
                least[column] = Math.Max(least[column], cellLeast);
                natural[column] = Math.Max(natural[column], cellNatural);
            }
            else
            {
                spanned.Add((column, span, cellLeast, cellNatural));
            }
        }

        foreach (var (column, span, cellLeast, cellNatural) in spanned)
        {
            Widen(least, column, span, cellLeast);
            Widen(natural, column, span, cellNatural);
        }

        for (var c = 0; c < count; c++)
        {
            natural[c] = Math.Max(natural[c], least[c]);
        }

        return (least, natural);
    }

    /// <summary>Поля шаблона вокруг сетки: рамки, отступы и поля элементов между презентером и сеткой.</summary>
    private double ChromeWidth()
    {
        var chrome = 0.0;
        for (var node = (DependencyObject?)_grid; node is not null && !ReferenceEquals(node, this); node = VisualTreeHelper.GetParent(node))
        {
            if (node is FrameworkElement element)
            {
                chrome += element.Margin.Left + element.Margin.Right;
            }

            if (node is Border border)
            {
                chrome += border.BorderThickness.Left + border.BorderThickness.Right + border.Padding.Left + border.Padding.Right;
            }
            else if (node is Control control)
            {
                chrome += control.BorderThickness.Left + control.BorderThickness.Right + control.Padding.Left + control.Padding.Right;
            }
        }

        return chrome;
    }

    private void Build()
    {
        if (Table is null || _grid == null)
        {
            return;
        }

        _grid.Children.Clear();
        _grid.RowDefinitions.Clear();
        _grid.ColumnDefinitions.Clear();
        _gridRows.Clear();
        _cells.Clear();
        _columns = null;

        for (var c = 0; c < Table.ColumnCount; c++)
            _grid.ColumnDefinitions.Add(new() { Width = GridLength.Auto });

        var gaps = GetValue(GapsProperty) as GapLayout ?? GapLayout.Empty;
        for (var r = 0; r < Table.Rows.Count; r++)
        {
            var row = Table.Rows[r];
            if (gaps.Before(row) is > 0 and var gap)
            {
                var space = new Border();
                space.SetResourceReference(Border.BackgroundProperty, "Md.Gap.Brush");
                Grid.SetRow(space, _grid.RowDefinitions.Count);
                Grid.SetColumnSpan(space, Math.Max(1, Table.ColumnCount));
                _grid.RowDefinitions.Add(new() { Height = new(gap) });
                _grid.Children.Add(space);
            }

            var gridRow = _grid.RowDefinitions.Count;
            _grid.RowDefinitions.Add(new() { Height = GridLength.Auto });
            _gridRows.Add(gridRow);

            var column = 0;
            foreach (var cell in row.Cells.TakeWhile(_ => column < Table.ColumnCount))
            {
                var span = Math.Clamp(cell.ColumnSpan, 1, Table.ColumnCount - column);
                AddCell(cell, row.IsHeader, gridRow, column, span);
                column += span;
            }

            for (; column < Table.ColumnCount; column++)
            {
                AddCell(new([]), row.IsHeader, gridRow, column, 1);
            }
        }
    }

    private void AddCell(TableCellVm cell, bool isHeader, int gridRow, int column, int span)
    {
        var presenter = new ContentPresenter
        {
            Content = cell,
            ContentTemplate = isHeader ? HeaderTemplate : CellTemplate
        };

        if (Table!.AlignmentOf(column) is not TableAlignment.Default and var alignment)
        {
            presenter.SetValue(TextBlock.TextAlignmentProperty,
                alignment switch
                {
                    TableAlignment.Center => TextAlignment.Center,
                    TableAlignment.Right => TextAlignment.Right,
                    _ => TextAlignment.Left
                });
        }

        Grid.SetRow(presenter, gridRow);
        Grid.SetColumn(presenter, column);
        Grid.SetColumnSpan(presenter, span);
        _grid!.Children.Add(presenter);
        _cells.Add((presenter, column, span));
    }
}