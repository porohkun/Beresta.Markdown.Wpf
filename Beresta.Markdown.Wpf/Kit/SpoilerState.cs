namespace Beresta.Markdown.Wpf.Kit;

using System.ComponentModel;
using Extensions;

/// <summary>Whether a spoiler is revealed; a click on it reveals and hides it.</summary>
public sealed class SpoilerState : INotifyPropertyChanged, IMdClickableState
{
    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The spoiler is revealed.</summary>
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

    /// <inheritdoc/>
    public void OnClick() => IsRevealed = !IsRevealed;
}
