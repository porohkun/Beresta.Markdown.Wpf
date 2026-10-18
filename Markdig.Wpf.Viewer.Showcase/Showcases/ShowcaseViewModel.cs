namespace MarkdigWpfViewer.Showcase.Showcases;

using Localization;

/// <summary>Витрина одного сценария: заголовок в списке слева и общие настройки.</summary>
public abstract class ShowcaseViewModel : BindableBase
{
    protected ShowcaseViewModel(ShowcaseOptions options)
    {
        Options = options;
        Localizer.Changed += OnLanguageChanged;
    }

    /// <summary>Заголовок на текущем языке: строка <c>&lt;Витрина&gt;.Title</c>, витрина — имя вьюмодели без <c>ViewModel</c>.</summary>
    public string Title => Localizer.Text($"{GetType().Name.Replace("ViewModel", string.Empty, StringComparison.Ordinal)}.Title");

    public ShowcaseOptions Options { get; }

    /// <summary>Язык сменился: все свойства перечитываются — заголовок и тексты уже на новом языке.</summary>
    protected virtual void OnLanguageChanged() => RaisePropertyChanged(string.Empty);
}