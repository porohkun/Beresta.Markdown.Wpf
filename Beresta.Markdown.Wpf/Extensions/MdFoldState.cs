namespace Beresta.Markdown.Wpf.Extensions;

using System.ComponentModel;

/// <summary>Whether a collapsible block or a callout is expanded; a widget state of the viewer.</summary>
public sealed class MdFoldState(bool isExpanded) : INotifyPropertyChanged
{
    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The block is expanded.</summary>
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
