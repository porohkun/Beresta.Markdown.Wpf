namespace MarkdigWpfViewer.Showcase;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using Theming;

/// <summary>Общие настройки всех витрин: тема и номера строк.</summary>
public sealed class ShowcaseOptions : INotifyPropertyChanged
{
    private bool _isDark;
    private bool _showSourceLineNumbers;
    private bool _showCodeLineNumbers;

    public event PropertyChangedEventHandler? PropertyChanged;

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
