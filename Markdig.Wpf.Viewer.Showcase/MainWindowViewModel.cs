namespace MarkdigWpfViewer.Showcase;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using Showcases;
using Showcases.CustomLook;
using Showcases.Document;
using Showcases.Extensions;
using Showcases.LargeDocument;
using Showcases.Preview;
using Showcases.TwoPanes;

/// <summary>Витрины, выбранная из них и общие настройки.</summary>
public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private ShowcaseViewModel _selected;

    public MainWindowViewModel()
    {
        Showcases =
        [
            new DocumentViewModel(Options),
            new ExtensionsViewModel(Options),
            new CustomLookViewModel(Options),
            new TwoPanesViewModel(Options),
            new LargeDocumentViewModel(Options),
            new PreviewViewModel(Options)
        ];

        _selected = Showcases[0];
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ShowcaseOptions Options { get; } = new();

    public IReadOnlyList<ShowcaseViewModel> Showcases { get; }

    public ShowcaseViewModel Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new(name));
}
