namespace MarkdigWpfViewer.Showcase.Showcases.LargeDocument;

/// <summary>Документ на тысячи блоков: создаются только видимые.</summary>
public sealed class LargeDocumentViewModel : ShowcaseViewModel
{
    private const int Sections = 300;
    private const int ParagraphsPerSection = 9;

    private static readonly string[] Sentences =
    [
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
        "Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.",
        "Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.",
        "Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.",
        "Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusantium doloremque laudantium, totam rem aperiam.",
        "Nemo enim ipsam voluptatem quia voluptas sit aspernatur aut odit aut fugit, sed quia consequuntur magni dolores eos qui ratione voluptatem sequi nesciunt.",
        "Neque porro quisquam est, qui dolorem ipsum quia dolor sit amet, consectetur, adipisci velit.",
        "Quis autem vel eum iure reprehenderit qui in ea voluptate velit esse quam nihil molestiae consequatur."
    ];

    public LargeDocumentViewModel(ShowcaseOptions options)
        : base(options)
    {
    }

    public override string Title => "Большой документ";

    /// <summary>Многократно повторённый lorem ipsum: разделы с заголовком и абзацами разной длины.</summary>
    public string Text { get; } = string.Join(
        "\n\n",
        Enumerable.Range(0, Sections * (ParagraphsPerSection + 1)).Select(Block));

    /// <summary>Блок по номеру: заголовок раздела либо абзац из одной-шести фраз.</summary>
    private static string Block(int index) => index % (ParagraphsPerSection + 1) == 0
        ? $"## Раздел {index / (ParagraphsPerSection + 1) + 1}"
        : string.Join(" ", Enumerable.Range(index, 1 + index % 6).Select(i => Sentences[i % Sentences.Length]));
}
