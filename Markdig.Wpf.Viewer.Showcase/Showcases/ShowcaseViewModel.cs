namespace MarkdigWpfViewer.Showcase.Showcases;

using System.ComponentModel;
using System.Runtime.CompilerServices;

/// <summary>Витрина одного сценария: заголовок в списке слева и общие настройки.</summary>
public abstract class ShowcaseViewModel : INotifyPropertyChanged
{
    protected ShowcaseViewModel(ShowcaseOptions options)
    {
        Options = options;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public abstract string Title { get; }

    public ShowcaseOptions Options { get; }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new(name));
}
