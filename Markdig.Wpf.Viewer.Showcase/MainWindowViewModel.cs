namespace MarkdigWpfViewer.Showcase;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using Theming;

/// <summary>Состояние витрины: тема, номера строк и тексты сценариев.</summary>
public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private bool _isDark;
    private bool _showSourceLineNumbers;
    private bool _showCodeLineNumbers;
    private string _previewText = Samples.Load("Document.md");

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Document { get; } = Samples.Load("Document.md");

    public string ExtensionsDocument { get; } = Samples.Load("Extensions.md");

    public string DiffLeft { get; } = Samples.Load("DiffLeft.md");

    public string DiffRight { get; } = Samples.Load("DiffRight.md");

    public string LargeDocument { get; } = string.Join(
        "\n\n",
        Enumerable.Range(1, 3000).Select(i => i % 10 == 0
            ? $"## Раздел {i / 10}"
            : $"Абзац {i}: текст, который нужен только для объёма."));

    public bool IsDark
    {
        get => _isDark;
        set
        {
            if (_isDark == value)
            {
                return;
            }

            _isDark = value;
            ThemeSwitcher.Apply(value);
            OnPropertyChanged();
        }
    }

    /// <summary>Текст живого предпросмотра: вьюер перерисовывает его на каждую правку.</summary>
    public string PreviewText
    {
        get => _previewText;
        set
        {
            _previewText = value;
            OnPropertyChanged();
        }
    }

    public bool ShowSourceLineNumbers
    {
        get => _showSourceLineNumbers;
        set
        {
            _showSourceLineNumbers = value;
            OnPropertyChanged();
        }
    }

    public bool ShowCodeLineNumbers
    {
        get => _showCodeLineNumbers;
        set
        {
            _showCodeLineNumbers = value;
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new(name));
}
