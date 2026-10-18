namespace MarkdigWpfViewer.Showcase.Localization;

using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

/// <summary>Строки витрины для привязок: индексатор по ключу и текущий язык, оба сообщают о его смене.</summary>
public sealed class LocalizedStrings : INotifyPropertyChanged
{
    private LocalizedStrings()
    {
        Localizer.Changed += () =>
        {
            PropertyChanged?.Invoke(this, new(Binding.IndexerName));
            PropertyChanged?.Invoke(this, new(nameof(Culture)));
        };
    }

    public static LocalizedStrings Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Текущий язык интерфейса.</summary>
    public CultureInfo Culture => CultureInfo.CurrentUICulture;

    public string this[string key] => Localizer.Text(key);
}
