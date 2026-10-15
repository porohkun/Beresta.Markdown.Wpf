namespace MarkdigWpfViewer.Showcase.Showcases.CustomLook;

/// <summary>Тот же документ в своём виде: свой шрифт и свой шаблон заголовка.</summary>
public sealed class CustomLookViewModel : ShowcaseViewModel
{
    public CustomLookViewModel(ShowcaseOptions options)
        : base(options)
    {
    }

    public override string Title => "Свой вид";

    public string Text => Samples.Document;
}
