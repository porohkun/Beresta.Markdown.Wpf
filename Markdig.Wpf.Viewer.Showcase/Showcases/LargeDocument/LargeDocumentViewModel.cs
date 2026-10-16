namespace MarkdigWpfViewer.Showcase.Showcases.LargeDocument;

/// <summary>Документ на тысячи блоков: создаются только видимые.</summary>
public sealed class LargeDocumentViewModel : ShowcaseViewModel
{
    public LargeDocumentViewModel(ShowcaseOptions options)
        : base(options)
    {
    }

    public override string Title => "Большой документ";

    public string Text { get; } = string.Join(
        "\n\n",
        Enumerable.Range(1, 3000).Select(i => i % 10 == 0
            ? $"## Раздел {i / 10}"
            : $"Абзац {i}: текст, который нужен только для объёма."));
}