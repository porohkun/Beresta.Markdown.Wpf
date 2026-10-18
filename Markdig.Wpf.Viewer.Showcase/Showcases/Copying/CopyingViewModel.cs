namespace MarkdigWpfViewer.Showcase.Showcases.Copying;

using Localization;

/// <summary>Копирование, отданное приложению: что класть в буфер, выбирается здесь, а рядом видно, что легло.</summary>
public sealed class CopyingViewModel : ShowcaseViewModel
{
    private const int Attempts = 10;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(50);

    private IReadOnlyList<string>? _formats;
    private bool _busy;

    public CopyingViewModel(ShowcaseOptions options)
        : base(options)
    {
        Choice = Choices[0];
    }

    public string Text => Samples.Document;

    public IReadOnlyList<CopyChoice> Choices { get; } =
    [
        new(CopyKind.Viewer, "Copying.Choice.Viewer"),
        new(CopyKind.Text, "Copying.Choice.Text"),
        new(CopyKind.Markdown, "Copying.Choice.Markdown")
    ];

    /// <summary>Что кладёт в буфер Ctrl+C.</summary>
    public CopyChoice Choice
    {
        get;
        set => Set(ref field, value);
    }

    /// <summary>Форматы, которые сейчас лежат в буфере, на текущем языке; буфер занят — об этом.</summary>
    public string Formats => _formats switch
    {
        { } formats => string.Format(Localizer.Text("Copying.Formats"), string.Join(", ", formats)),
        null when _busy => Localizer.Text("Copying.ClipboardBusy"),
        null => string.Empty
    };

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
                SetFormats(snapshot.Formats, busy: false);
                PlainText = snapshot.Text;
                Markdown = snapshot.Markdown;
                return;
            }

            // BYDESIGN: только что изменённый буфер тут же читают журнал буфера Windows и другие
            // программы — пока он открыт у них, чтение не удаётся, и его повторяют.
            await Task.Delay(RetryDelay);
        }

        SetFormats(null, busy: true);
    }

    /// <summary>Запоминает форматы буфера; прочитать его не удалось — <paramref name="busy"/>.</summary>
    private void SetFormats(IReadOnlyList<string>? formats, bool busy)
    {
        _formats = formats;
        _busy = busy;
        RaisePropertyChanged(nameof(Formats));
    }
}