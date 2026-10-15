namespace MarkdigWpfViewer.Showcase.Showcases.Extensions;

/// <summary>Состояние заметки, которое переживает её контрол: развёрнута ли она.</summary>
public sealed class NoteState : BindableBase
{
    public bool IsExpanded
    {
        get;
        set => Set(ref field, value);
    } = true;
}