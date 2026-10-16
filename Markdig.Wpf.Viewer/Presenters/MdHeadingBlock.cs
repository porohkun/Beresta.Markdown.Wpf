namespace MarkdigWpfViewer.Presenters;

using System.Windows;
using System.Windows.Automation;

/// <summary>Текст заголовка; для автоматизации — заголовок своего уровня.</summary>
public sealed class MdHeadingBlock : MdTextBlock
{
    public static readonly DependencyProperty LevelProperty =
        DependencyProperty.Register(
            nameof(Level),
            typeof(int),
            typeof(MdHeadingBlock),
            new FrameworkPropertyMetadata(1, (d, e) => SetHeadingLevel(d, (int)e.NewValue)));

    public MdHeadingBlock()
    {
        SetHeadingLevel(this, Level);
    }

    public int Level
    {
        get => (int)GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    private static void SetHeadingLevel(DependencyObject heading, int level)
        => AutomationProperties.SetHeadingLevel(heading, AutomationHeadingLevel.Level1 + Math.Clamp(level, 1, 9) - 1);
}