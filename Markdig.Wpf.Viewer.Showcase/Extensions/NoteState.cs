namespace MarkdigWpfViewer.Showcase.Extensions;

using System.ComponentModel;
using System.Runtime.CompilerServices;

/// <summary>Состояние заметки, которое переживает её контрол: развёрнута ли она.</summary>
public sealed class NoteState : INotifyPropertyChanged
{
    private bool _isExpanded = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            _isExpanded = value;
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new(name));
}