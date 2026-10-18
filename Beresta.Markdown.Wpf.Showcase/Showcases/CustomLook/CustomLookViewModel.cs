namespace Beresta.Markdown.Wpf.Showcase.Showcases.CustomLook;

/// <summary>Тот же документ в своём виде: свой шрифт, свой шаблон заголовка и ключи темы, которые правятся на лету.</summary>
public sealed class CustomLookViewModel : ShowcaseViewModel
{
    public CustomLookViewModel(ShowcaseOptions options)
        : base(options)
    {
        options.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ShowcaseOptions.IsDark))
            {
                Keys.Refresh();
            }
        };
    }

    public string Text => Samples.Document;

    /// <summary>Ключи темы вьюера со своими значениями.</summary>
    public ThemeKeys Keys { get; } = new();

    /// <summary>Свой шрифт вьюера; иначе — шрифт приложения.</summary>
    public bool IsCustomFont
    {
        get;
        set => Set(ref field, value);
    } = true;

    /// <summary>Свой шаблон заголовка; иначе — шаблон темы.</summary>
    public bool IsCustomHeadings
    {
        get;
        set => Set(ref field, value);
    } = true;
}
