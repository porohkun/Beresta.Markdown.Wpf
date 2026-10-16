namespace MarkdigWpfViewer.Showcase.Showcases.Kit;

/// <summary>Готовые расширения библиотеки: синтаксис других программ, подключённый одной строкой.</summary>
public sealed class KitViewModel : ShowcaseViewModel
{
    public KitViewModel(ShowcaseOptions options)
        : base(options)
    {
    }

    public override string Title => "Готовые расширения";

    public string Text
    {
        get;
        set => Set(ref field, value);
    } = Samples.Near<KitViewModel>("Kit.md");

    /// <summary>Заменяет кусок исходника: решение по правке, переключённая задача.</summary>
    public void Replace(int start, int length, string replacement)
    {
        if (start + length <= Text.Length)
        {
            Text = Text[..start] + replacement + Text[(start + length)..];
        }
    }

    /// <summary>Последняя нажатая ссылка своей схемы: <c>wiki:</c>, <c>tag:</c>, <c>mention:</c>.</summary>
    public string LastLink
    {
        get;
        set => Set(ref field, value);
    } = "Нажми вики-ссылку, тег или упоминание — здесь появится их цель.";
}
