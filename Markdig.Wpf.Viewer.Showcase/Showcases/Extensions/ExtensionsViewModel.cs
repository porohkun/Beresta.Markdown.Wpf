namespace MarkdigWpfViewer.Showcase.Showcases.Extensions;

/// <summary>Свой синтаксис: заметка со своим состоянием, упоминание со своим разбором и чипы оформленным текстом.</summary>
public sealed class ExtensionsViewModel : ShowcaseViewModel
{
    public ExtensionsViewModel(ShowcaseOptions options)
        : base(options)
    {
    }

    public string Text => Samples.Near<ExtensionsViewModel>("Extensions.md");
}