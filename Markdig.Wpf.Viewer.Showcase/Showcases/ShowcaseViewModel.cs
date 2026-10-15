namespace MarkdigWpfViewer.Showcase.Showcases;

/// <summary>Витрина одного сценария: заголовок в списке слева и общие настройки.</summary>
public abstract class ShowcaseViewModel : BindableBase
{
    protected ShowcaseViewModel(ShowcaseOptions options)
    {
        Options = options;
    }

    public abstract string Title { get; }

    public ShowcaseOptions Options { get; }
}