namespace MarkdigWpfViewer.Presenters;

using System.Windows;

public sealed class MdHeadingBlock : MdTextBlock
{
    public static readonly DependencyProperty LevelProperty =
        DependencyProperty.Register(
            nameof(Level),
            typeof(int),
            typeof(MdHeadingBlock),
            new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsMeasure, OnAnyPropertyChanged));

    public int Level
    {
        get => (int)GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }
}