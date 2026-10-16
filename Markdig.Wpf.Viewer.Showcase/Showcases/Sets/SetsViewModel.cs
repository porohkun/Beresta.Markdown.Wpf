namespace MarkdigWpfViewer.Showcase.Showcases.Sets;

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
            new("GitHub", Samples.Near<SetsViewModel>("GitHub.md"), github),
            new("GitLab", Samples.Near<SetsViewModel>("GitLab.md"), gitlab),
            new("Obsidian", Samples.Near<SetsViewModel>("Obsidian.md"), new ObsidianSet { Resolver = SetsResolver.Instance }),
            new("MkDocs", Samples.Near<SetsViewModel>("MkDocs.md"), new MkDocsSet()),
            new("Docusaurus и VuePress", Samples.Near<SetsViewModel>("Docusaurus.md"), new DocusaurusSet()),
            new("HackMD", Samples.Near<SetsViewModel>("HackMD.md"), new HackMDSet()),
            new("Чат", Samples.Near<SetsViewModel>("Chat.md"), new ChatSet { Resolver = SetsResolver.Instance })
        ];
        Selected = Dialects[0];
    }

    public override string Title => "Наборы";

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

    /// <summary>Последняя нажатая ссылка своей схемы: <c>wiki:</c>, <c>tag:</c>, <c>mention:</c>.</summary>
    public string LastLink
    {
        get;
        set => Set(ref field, value);
    } = "Нажми вики-ссылку, тег или упоминание — здесь появится их цель.";
}
