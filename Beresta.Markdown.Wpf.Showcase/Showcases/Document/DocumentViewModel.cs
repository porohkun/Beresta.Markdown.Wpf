namespace Beresta.Markdown.Wpf.Showcase.Showcases.Document;

/// <summary>Документ со всем, что умеет вьюер: оглавление, подсветка найденного, выделение, задачи.</summary>
public sealed class DocumentViewModel : ShowcaseViewModel
{
    public DocumentViewModel(ShowcaseOptions options)
        : base(options)
    {
    }

    public string Text => Samples.Document;

    /// <summary>Флажки задач и сворачивание правят текст документа.</summary>
    public bool AreControlsEditable
    {
        get;
        set => Set(ref field, value);
    }
}