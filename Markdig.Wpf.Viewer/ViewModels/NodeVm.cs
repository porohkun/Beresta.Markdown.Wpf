namespace MarkdigWpfViewer.ViewModels;

/// <summary>Элемент модели документа с позицией в исходнике.</summary>
public abstract record NodeVm
{
    /// <summary>Откуда элемент взят в исходном тексте.</summary>
    public MdSourceSpan Source { get; init; }

    /// <summary>
    /// Устойчивый идентификатор: переживает новый текст, пока элемент не изменился или изменился
    /// на своём месте. 0 — у элемента без своего узла разбора.
    /// </summary>
    public long NodeId { get; init; }
}
