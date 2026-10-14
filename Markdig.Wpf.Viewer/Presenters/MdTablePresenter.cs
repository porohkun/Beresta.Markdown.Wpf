namespace MarkdigWpfViewer.Presenters;

using System.Windows;
using System.Windows.Controls;
using ViewModels;

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

    private Grid? _grid;

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

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _grid = GetTemplateChild("PART_Grid") as Grid;

        Build();
    }

    private static void OnAnyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MdTablePresenter)d).Build();

    private void Build()
    {
        if (Table is null || _grid == null)
        {
            return;
        }

        _grid.Children.Clear();
        _grid.RowDefinitions.Clear();
        _grid.ColumnDefinitions.Clear();

        for (var c = 0; c < Table.ColumnCount; c++)
            _grid.ColumnDefinitions.Add(new() { Width = GridLength.Auto });

        for (var r = 0; r < Table.Rows.Count; r++)
        {
            _grid.RowDefinitions.Add(new() { Height = GridLength.Auto });

            var row = Table.Rows[r];
            for (var c = 0; c < Table.ColumnCount; c++)
            {
                var cell = c < row.Cells.Count ? row.Cells[c] : new([]);

                var presenter = new ContentPresenter
                {
                    Content = cell,
                    ContentTemplate = row.IsHeader ? HeaderTemplate : CellTemplate
                };

                Grid.SetRow(presenter, r);
                Grid.SetColumn(presenter, c);
                _grid.Children.Add(presenter);
            }
        }
    }
}