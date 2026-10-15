namespace MarkdigWpfViewer.ViewModels;

/// <summary>Элемент модели документа с позицией в исходнике.</summary>
public abstract record NodeVm
{
    /// <summary>Откуда элемент взят в исходном тексте.</summary>
    public MdSourceSpan Source { get; init; }
}
