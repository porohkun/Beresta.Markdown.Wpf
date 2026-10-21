namespace Beresta.Markdown.Wpf.Showcase;

using Editor;
using Localization;
using Theming;

/// <summary>Общие настройки всех витрин: тема, язык, номера строк, подсветка кода, выделение.</summary>
public sealed class ShowcaseOptions : BindableBase
{
    public ShowcaseOptions()
    {
        IsRussian = Localizer.IsSystemRussian;
    }

    public bool IsDark
    {
        get;
        set => Set(ref field, value, () => ThemeSwitcher.Apply(value));
    }

    /// <summary>Витрина по-русски; иначе — по-английски. При запуске — язык системы.</summary>
    public bool IsRussian
    {
        get;
        set => Set(ref field, value, () =>
        {
            Localizer.Apply(value);
            RaisePropertyChanged(nameof(IsEnglish));
        });
    }

    /// <summary>Витрина по-английски — обратное <see cref="IsRussian"/>, для переключателя.</summary>
    public bool IsEnglish
    {
        get => !IsRussian;
        set => IsRussian = !value;
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

    public bool HighlightCode
    {
        get;
        set => Set(ref field, value, () => RaisePropertyChanged(nameof(CodeHighlighter)));
    } = true;

    /// <summary>Подсветчик кода всех вьюеров и редакторов витрины: подсветчик AvalonEdit или никакого.</summary>
    public IMdCodeHighlighter? CodeHighlighter => HighlightCode ? MdAvalonCodeHighlighter.Instance : null;

    public bool IsSelectionEnabled
    {
        get;
        set => Set(ref field, value);
    } = true;
}