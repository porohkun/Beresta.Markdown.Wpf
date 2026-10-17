namespace MarkdigWpfViewer.Showcase;

using Showcases;
using Showcases.About;
using Showcases.Chat;
using Showcases.Copying;
using Showcases.CustomLook;
using Showcases.Document;
using Showcases.Extensions;
using Showcases.Kit;
using Showcases.LargeDocument;
using Showcases.MarginButtons;
using Showcases.Preview;
using Showcases.Sets;
using Showcases.Streaming;
using Showcases.TwoPanes;

/// <summary>Витрины, выбранная из них и общие настройки.</summary>
public sealed class MainWindowViewModel : BindableBase
{
    public MainWindowViewModel()
    {
        Showcases =
        [
            new DocumentViewModel(Options),
            new ExtensionsViewModel(Options),
            new KitViewModel(Options),
            new SetsViewModel(Options),
            new CustomLookViewModel(Options),
            new TwoPanesViewModel(Options),
            new MarginButtonsViewModel(Options),
            new LargeDocumentViewModel(Options),
            new ChatViewModel(Options),
            new StreamingViewModel(Options),
            new PreviewViewModel(Options),
            new CopyingViewModel(Options),
            new AboutViewModel(Options)
        ];

        Selected = Showcases[0];
    }

    public ShowcaseOptions Options { get; } = new();

    public IReadOnlyList<ShowcaseViewModel> Showcases { get; }

    public ShowcaseViewModel Selected
    {
        get;
        set => Set(ref field, value);
    }
}