namespace MarkdigWpfViewer.Showcase.Showcases.Copying;

/// <summary>Копирование, отданное приложению: что класть в буфер, выбирается здесь, а рядом видно, что легло.</summary>
public sealed class CopyingViewModel : ShowcaseViewModel
{
    private const int Attempts = 10;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(50);

    public CopyingViewModel(ShowcaseOptions options)
        : base(options)
    {
        Choice = Choices[0];
    }

    public override string Title => "Копирование";

    public string Text => Samples.Document;

    public IReadOnlyList<CopyChoice> Choices { get; } =
    [
        new(CopyKind.Viewer, "Оба вида, как вьюер"),
        new(CopyKind.Text, "Только текст"),
        new(CopyKind.Markdown, "Разметку обычным текстом")
    ];

    /// <summary>Что кладёт в буфер Ctrl+C.</summary>
    public CopyChoice Choice
    {
        get;
        set => Set(ref field, value);
    }

    /// <summary>Форматы, которые сейчас лежат в буфере.</summary>
    public string Formats
    {
        get;
        private set => Set(ref field, value);
    } = string.Empty;

    /// <summary>Обычный текст из буфера.</summary>
    public string PlainText
    {
        get;
        private set => Set(ref field, value);
    } = string.Empty;

    /// <summary>Разметка из буфера.</summary>
    public string Markdown
    {
        get;
        private set => Set(ref field, value);
    } = string.Empty;

    /// <summary>Перечитывает буфер обмена.</summary>
    public async Task RefreshAsync()
    {
        for (var attempt = 0; attempt < Attempts; attempt++)
        {
            if (ClipboardSnapshot.TryRead() is { } snapshot)
            {
                Formats = $"Форматы: {string.Join(", ", snapshot.Formats)}";
                PlainText = snapshot.Text;
                Markdown = snapshot.Markdown;
                return;
            }

            // BYDESIGN: только что изменённый буфер тут же читают журнал буфера Windows и другие
            // программы — пока он открыт у них, чтение не удаётся, и его повторяют.
            await Task.Delay(RetryDelay);
        }

        Formats = "Буфер обмена занят другой программой";
    }
}
