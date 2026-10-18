namespace Beresta.Markdown.Wpf.Presenters;

using System.Windows;
using System.Windows.Automation;

/// <summary>The text of a heading; for UI Automation — a heading of its level.</summary>
public sealed class MdHeadingBlock : MdTextBlock
{
    /// <summary>The level of the heading, from 1 to 6.</summary>
    public static readonly DependencyProperty LevelProperty =
        DependencyProperty.Register(
            nameof(Level),
            typeof(int),
            typeof(MdHeadingBlock),
            new FrameworkPropertyMetadata(1, (d, e) => SetHeadingLevel(d, (int)e.NewValue)));

    /// <summary>Creates an empty heading.</summary>
    public MdHeadingBlock()
    {
        SetHeadingLevel(this, Level);
    }

    /// <inheritdoc cref="LevelProperty"/>
    public int Level
    {
        get => (int)GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    private static void SetHeadingLevel(DependencyObject heading, int level)
        => AutomationProperties.SetHeadingLevel(heading, AutomationHeadingLevel.Level1 + Math.Clamp(level, 1, 9) - 1);
}