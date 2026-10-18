namespace MarkdigWpfViewer.Showcase.Showcases.About;

using System.IO;

/// <summary>О витрине: что это и уведомления о лицензиях сторонних библиотек.</summary>
public sealed class AboutViewModel : ShowcaseViewModel
{
    private const string NoticesFile = "THIRD-PARTY-NOTICES.md";

    public AboutViewModel(ShowcaseOptions options)
        : base(options)
    {
    }

    /// <summary>Описание витрины и уведомления из файла, который лежит рядом с программой.</summary>
    public string Text { get; } =
        Samples.Near<AboutViewModel>("About.md")
        + File.ReadAllText(Path.Combine(AppContext.BaseDirectory, NoticesFile));
}