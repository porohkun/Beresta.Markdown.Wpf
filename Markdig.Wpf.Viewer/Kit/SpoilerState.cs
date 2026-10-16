namespace MarkdigWpfViewer.Kit;

using System.ComponentModel;
using Extensions;

/// <summary>Раскрыт ли спойлер; щелчок по нему раскрывает и прячет.</summary>
public sealed class SpoilerState : INotifyPropertyChanged, IMdClickableState
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsRevealed
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            PropertyChanged?.Invoke(this, new(nameof(IsRevealed)));
        }
    }

    public void OnClick() => IsRevealed = !IsRevealed;
}
