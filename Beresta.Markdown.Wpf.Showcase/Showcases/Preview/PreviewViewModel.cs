namespace Beresta.Markdown.Wpf.Showcase.Showcases.Preview;

/// <summary>Живой предпросмотр: неизменившиеся блоки переживают правку, видимое стоит на месте.</summary>
public sealed class PreviewViewModel : ShowcaseViewModel
{
    public PreviewViewModel(ShowcaseOptions options)
        : base(options)
    {
    }

    /// <summary>Текст из редактора; вьюер перерисовывает его на каждую правку.</summary>
    public string Text
    {
        get;
        set => Set(ref field, value);
    } = Samples.Document;

    /// <summary>Язык сменился: правка редактора уступает образцу на новом языке.</summary>
    protected override void OnLanguageChanged()
    {
        Text = Samples.Document;
        base.OnLanguageChanged();
    }
}