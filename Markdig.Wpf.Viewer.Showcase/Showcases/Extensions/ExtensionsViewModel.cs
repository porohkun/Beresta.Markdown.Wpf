namespace MarkdigWpfViewer.Showcase.Showcases.Extensions;

/// <summary>Свой синтаксис: заметка со своим состоянием и упоминание со своим разбором.</summary>
public sealed class ExtensionsViewModel : ShowcaseViewModel
{
    public ExtensionsViewModel(ShowcaseOptions options)
        : base(options)
    {
    }

    public override string Title => "Расширения";

    public string Text { get; } = Samples.Near<ExtensionsViewModel>("Extensions.md");
}
