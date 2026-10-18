namespace Beresta.Markdown.Wpf.Behaviors;

/// <summary>The edge of the scrolling it sticks to: where new data comes.</summary>
public enum StickyEdge
{
    /// <summary>Does not stick.</summary>
    None,

    /// <summary>To the bottom: new content is added at the end.</summary>
    Bottom,

    /// <summary>To the top: new content comes at the start.</summary>
    Top
}
