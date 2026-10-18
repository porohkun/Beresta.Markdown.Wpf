namespace MarkdigWpfViewer.Showcase.Showcases;

using Localization;

/// <summary>Витрина одного сценария: заголовок в списке слева и общие настройки.</summary>
public abstract class ShowcaseViewModel : BindableBase
{
    protected ShowcaseViewModel(ShowcaseOptions options)
    {
        Options = options;
        Localizer.Changed += () => RaisePropertyChanged(nameof(Title));
    }

    /// <summary>Заголовок на текущем языке: строка <c>&lt;Витрина&gt;.Title</c>, витрина — имя вьюмодели без <c>ViewModel</c>.</summary>
    public string Title => Localizer.Text($"{GetType().Name.Replace("ViewModel", string.Empty, StringComparison.Ordinal)}.Title");

    public ShowcaseOptions Options { get; }
}