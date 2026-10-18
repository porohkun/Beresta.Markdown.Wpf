namespace MarkdigWpfViewer.Showcase.Showcases.Document;

/// <summary>Документ со всем, что умеет вьюер: оглавление, подсветка найденного, выделение, задачи.</summary>
public sealed class DocumentViewModel : ShowcaseViewModel
{
    public DocumentViewModel(ShowcaseOptions options)
        : base(options)
    {
    }

    public string Text => Samples.Document;
}