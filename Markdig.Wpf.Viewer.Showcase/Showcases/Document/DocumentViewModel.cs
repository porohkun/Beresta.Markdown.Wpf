namespace MarkdigWpfViewer.Showcase.Showcases.Document;

using ViewModels;

/// <summary>Документ со всем, что умеет вьюер: оглавление, подсветка найденного, выделение, задачи.</summary>
public sealed class DocumentViewModel : ShowcaseViewModel
{
    public DocumentViewModel(ShowcaseOptions options)
        : base(options)
    {
    }

    public override string Title => "Документ";

    public string Text
    {
        get;
        private set => Set(ref field, value);
    } = Samples.Document;

    /// <summary>Нажатый флажок задачи правит исходник: <c>[ ]</c> или <c>[x]</c> на месте задачи.</summary>
    public void Toggle(TaskVm task, bool isChecked)
    {
        var source = task.Source;
        if (source.End <= Text.Length)
        {
            Text = Text[..source.Start] + (isChecked ? "[x]" : "[ ]") + Text[source.End..];
        }
    }
}