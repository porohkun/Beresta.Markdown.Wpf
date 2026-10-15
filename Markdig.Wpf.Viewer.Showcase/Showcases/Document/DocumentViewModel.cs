namespace MarkdigWpfViewer.Showcase.Showcases.Document;

/// <summary>Документ со всем, что умеет вьюер: подсветка найденного и выделение.</summary>
public sealed class DocumentViewModel : ShowcaseViewModel
{
    public DocumentViewModel(ShowcaseOptions options)
        : base(options)
    {
    }

    public override string Title => "Документ";

    public string Text => Samples.Document;
}
