namespace MarkdigWpfViewer.Showcase.Localization;

using System.ComponentModel;
using System.Windows.Data;

/// <summary>Строки витрины для привязок: индексатор по ключу, который сообщает о смене языка.</summary>
public sealed class LocalizedStrings : INotifyPropertyChanged
{
    private LocalizedStrings()
    {
        Localizer.Changed += () => PropertyChanged?.Invoke(this, new(Binding.IndexerName));
    }

    public static LocalizedStrings Instance { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key] => Localizer.Text(key);
}
