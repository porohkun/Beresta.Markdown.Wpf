namespace MarkdigWpfViewer.Showcase.Showcases.Sets;

using Localization;
using MarkdigWpfViewer.Kit;

/// <summary>Наборы готовых расширений: документ в синтаксисе каждой программы и её набор.</summary>
public sealed class SetsViewModel : ShowcaseViewModel
{
    public SetsViewModel(ShowcaseOptions options)
        : base(options)
    {
        var github = new GitHubSet { Resolver = SetsResolver.Instance };
        github.IssueTemplates["#"] = "https://github.com/porohkun/Markdig.Wpf.Viewer/issues/{0}";
        var gitlab = new GitLabSet();
        gitlab.IssueTemplates["#"] = "https://gitlab.com/gitlab-org/gitlab/-/issues/{0}";

        Dialects =
        [
            new("GitHub", github),
            new("GitLab", gitlab),
            new("Obsidian", new ObsidianSet { Resolver = SetsResolver.Instance }),
            new("MkDocs", new MkDocsSet()),
            new("Docusaurus", new DocusaurusSet()),
            new("HackMD", new HackMDSet()),
            new("Chat", new ChatSet { Resolver = SetsResolver.Instance })
        ];
        Selected = Dialects[0];
    }

    public IReadOnlyList<Dialect> Dialects { get; }

    public Dialect Selected
    {
        get;
        set => Set(ref field, value);
    }

    /// <summary>Набор диалекта подключён; выключен — тот же документ без набора.</summary>
    public bool IsSetOn
    {
        get;
        set => Set(ref field, value);
    } = true;

    /// <summary>Цель последней нажатой ссылки своей схемы: <c>wiki:</c>, <c>tag:</c>, <c>mention:</c>; не нажимали — <c>null</c>.</summary>
    public string? LastHref
    {
        get;
        set => Set(ref field, value, () => RaisePropertyChanged(nameof(LastLink)));
    }

    /// <summary>Подсказка нажать ссылку либо её цель — на текущем языке.</summary>
    public string LastLink => LastHref is { } href
        ? string.Format(Localizer.Text("Links.Clicked"), href)
        : Localizer.Text("Links.Prompt");
}
