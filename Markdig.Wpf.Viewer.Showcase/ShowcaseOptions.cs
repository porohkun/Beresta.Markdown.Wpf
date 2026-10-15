namespace MarkdigWpfViewer.Showcase;

using Theming;

/// <summary>Общие настройки всех витрин: тема, номера строк, выделение.</summary>
public sealed class ShowcaseOptions : BindableBase
{
    public bool IsDark
    {
        get;
        set => Set(ref field, value, () => ThemeSwitcher.Apply(value));
    }

    public bool ShowSourceLineNumbers
    {
        get;
        set => Set(ref field, value);
    }

    public bool ShowCodeLineNumbers
    {
        get;
        set => Set(ref field, value);
    }

    public bool IsSelectionEnabled
    {
        get;
        set => Set(ref field, value);
    } = true;
}