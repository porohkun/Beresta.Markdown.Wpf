namespace MarkdigWpfViewer.Behaviors;

/// <summary>Край прокрутки, к которому она прилипает: откуда приходят новые данные.</summary>
public enum StickyEdge
{
    /// <summary>Не прилипает.</summary>
    None,

    /// <summary>К низу: новое дописывается в конец.</summary>
    Bottom,

    /// <summary>К верху: новое встаёт в начало.</summary>
    Top
}
