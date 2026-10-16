namespace MarkdigWpfViewer.Extensions;

using System.ComponentModel;

/// <summary>Развёрнут ли сворачиваемый блок или плашка; состояние виджета вьюера.</summary>
public sealed class MdFoldState(bool isExpanded) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsExpanded
    {
        get => isExpanded;
        set
        {
            if (isExpanded == value)
            {
                return;
            }

            isExpanded = value;
            PropertyChanged?.Invoke(this, new(nameof(IsExpanded)));
        }
    }
}
