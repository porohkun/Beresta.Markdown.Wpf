namespace Beresta.Markdown.Wpf.Showcase.Showcases.Editor;

using Beresta.Markdown.Wpf.Editor;

/// <summary>Редактор: вьюер, который правит исходник своего документа.</summary>
public sealed class EditorViewModel : ShowcaseViewModel
{
    public EditorViewModel(ShowcaseOptions options)
        : base(options)
    {
    }

    /// <summary>Текст документа; редактор отдаёт сюда свои правки.</summary>
    public string Text
    {
        get;
        set => Set(ref field, value);
    } = Samples.Near<EditorViewModel>("Editor.md");

    /// <summary>Режим правки редактора.</summary>
    public MdEditMode Mode
    {
        get;
        set => Set(ref field, value);
    } = MdEditMode.Blocks;

    /// <summary>Язык сменился: правки уступают образцу на новом языке.</summary>
    protected override void OnLanguageChanged()
    {
        Text = Samples.Near<EditorViewModel>("Editor.md");
        base.OnLanguageChanged();
    }
}
