namespace MarkdigWpfViewer.Tests;

using System.Windows.Markup;
using Extensions;
using Kit;
using ViewModels;

/// <summary>Наборы готовых расширений: состав по порядку, выключение частей, настройки, документ в синтаксисе программы.</summary>
public sealed class KitSetTests
{
    public static TheoryData<KitSet, Type[]> Sets => new()
    {
        { new GitHubSet(), [typeof(PropertiesCard), typeof(Mentions), typeof(IssueLinks), typeof(ColorChips), typeof(EmojiShortcodes)] },
        { new GitLabSet(), [typeof(TableOfContents), typeof(IssueLinks), typeof(ColorChips), typeof(EmojiShortcodes)] },
        {
            new ObsidianSet(),
            [typeof(PropertiesCard), typeof(ObsidianCallouts), typeof(Comments), typeof(WikiEmbeds), typeof(WikiLinks), typeof(Tags), typeof(TaskStatuses)]
        },
        { new MkDocsSet(), [typeof(MkDocsAdmonitions)] },
        { new DocusaurusSet(), [typeof(CalloutContainers)] },
        { new HackMDSet(), [typeof(CalloutContainers), typeof(TableOfContents), typeof(Shortcodes)] },
        { new ChatSet(), [typeof(Mentions), typeof(Spoilers), typeof(EmojiShortcodes)] }
    };

    [Theory]
    [MemberData(nameof(Sets))]
    public void Набор_собран_из_своих_частей_по_порядку(KitSet set, Type[] parts)
    {
        set.Extensions.Select(extension => extension.GetType()).Should().Equal(parts);
    }

    [Fact]
    public void Выключенные_части_не_подключаются_а_порядок_остальных_прежний()
    {
        var set = new GitHubSet { Without = KitParts.Mentions | KitParts.EmojiShortcodes };

        set.Extensions.Select(extension => extension.GetType()).Should().Equal(typeof(PropertiesCard), typeof(IssueLinks), typeof(ColorChips));

        set.Without = KitParts.ColorChips;
        set.Extensions.Select(extension => extension.GetType())
            .Should().Equal(typeof(PropertiesCard), typeof(Mentions), typeof(IssueLinks), typeof(EmojiShortcodes));
    }

    [Fact]
    public void Расширение_приложения_в_наборе_остаётся_за_частями()
    {
        var own = new Chips();
        var set = new ChatSet();
        set.Extensions.Add(own);

        set.Without = KitParts.Spoilers;

        set.Extensions.Should().HaveCount(3).And.EndWith(own);
    }

    [Fact]
    public void Части_выключаются_атрибутом_разметки()
    {
        WpfHost.Invoke(() =>
        {
            var set = (ObsidianSet)XamlReader.Parse(
                """<kit:ObsidianSet xmlns:kit="clr-namespace:MarkdigWpfViewer.Kit;assembly=Markdig.Wpf.Viewer" Without="Comments, TaskStatuses" />""");

            set.Without.Should().Be(KitParts.Comments | KitParts.TaskStatuses);
            set.Extensions.Should().HaveCount(5).And.NotContain(extension => extension is Comments || extension is TaskStatuses);
        });
    }

    [Fact]
    public void Документ_GitHub()
    {
        var set = new GitHubSet { Resolver = new Names() };
        set.IssueTemplates["#"] = "https://github.com/org/repo/issues/{0}";

        var blocks = Parse("---\ntitle: Notes\n---\n\nThanks @ivan for #12: `#0969DA` :tada:", set);

        blocks.OfType<PropertiesVm>().Should().ContainSingle();
        var inlines = blocks.OfType<ParagraphVm>().Single().Inlines;
        inlines.OfType<MentionVm>().Single().DisplayName.Should().Be("Иван");
        inlines.OfType<RunVm>().Single(run => run.Text == "#12").Href.Should().Be("https://github.com/org/repo/issues/12");
        inlines.OfType<ColorCodeVm>().Should().ContainSingle();
        ModelDump.Text(inlines).Should().EndWith("🎉");
    }

    [Fact]
    public void Документ_GitLab()
    {
        var set = new GitLabSet();
        set.IssueTemplates["#"] = "https://gitlab.com/group/project/-/issues/{0}";

        var blocks = Parse("[[_TOC_]]\n\n# Title\n\nFixes #7 in `rgb(0, 0, 0)` :rocket:", set);

        blocks.OfType<TocVm>().Should().ContainSingle();
        var inlines = blocks.OfType<ParagraphVm>().Single().Inlines;
        inlines.OfType<RunVm>().Single(run => run.Text == "#7").Href.Should().Be("https://gitlab.com/group/project/-/issues/7");
        inlines.OfType<ColorCodeVm>().Should().ContainSingle();
        ModelDump.Text(inlines).Should().EndWith("🚀");
    }

    [Fact]
    public void Документ_Obsidian()
    {
        var set = new ObsidianSet { Resolver = new Names() };

        var blocks = Parse(
            "---\ntags: [a]\n---\n\n> [!tip] Title\n> Body\n\nSee [[Known]] and [[Missing]] #tag %%hidden%% ![[pic.png|100]]\n\n- [/] doing",
            set);

        blocks.OfType<PropertiesVm>().Should().ContainSingle();
        blocks.OfType<CalloutVm>().Single().Kind.Should().Be("tip");
        var inlines = blocks.OfType<ParagraphVm>().Single().Inlines;
        inlines.OfType<RunVm>().Where(run => run.StyleName is not null).Select(run => (run.Text, run.StyleName))
            .Should().Equal(("Known", "WikiLink"), ("Missing", "WikiLink.Missing"), ("#tag", "Tag"));
        inlines.OfType<ImageVm>().Single().Width.Should().Be(100);
        ModelDump.Text(inlines).Should().NotContain("hidden");
        blocks.OfType<ListVm>().Single().Items[0].Blocks.OfType<ParagraphVm>().Single().Inlines.OfType<TaskVm>().Single().Status.Should().Be('/');
    }

    [Fact]
    public void Документ_MkDocs()
    {
        Parse("!!! note \"Title\"\n    Body", new MkDocsSet()).OfType<CalloutVm>().Single().Kind.Should().Be("note");
    }

    [Fact]
    public void Документ_Docusaurus()
    {
        Parse(":::tip Title\nBody\n:::", new DocusaurusSet()).OfType<CalloutVm>().Single().Kind.Should().Be("tip");
    }

    [Fact]
    public void Документ_HackMD()
    {
        var set = new HackMDSet { LoadThumbnails = false };
        set.ShortcodeHandlers["badge"] = shortcode => new RunVm(shortcode.Arguments[0]);

        var blocks = Parse("[TOC]\n\n# Title\n\n:::info\nBody\n:::\n\n{%youtube abc123xyz %} {%badge new %}", set);

        blocks.OfType<TocVm>().Should().ContainSingle();
        blocks.OfType<CalloutVm>().Single().Kind.Should().Be("info");
        var inlines = blocks.OfType<ParagraphVm>().Single().Inlines;
        inlines.OfType<VideoCardVm>().Single().Preview.Should().BeNull();
        ModelDump.Text(inlines).Should().EndWith(" new");
    }

    [Fact]
    public void Документ_чата()
    {
        var blocks = Parse("@ivan look ||secret|| :smile:", new ChatSet { Resolver = new Names() });

        var inlines = blocks.OfType<ParagraphVm>().Single().Inlines;
        inlines.OfType<MentionVm>().Single().DisplayName.Should().Be("Иван");
        inlines.OfType<RunVm>().Single(run => run.StyleName == "Spoiler").Text.Should().Be("secret");
        ModelDump.Text(inlines).Should().EndWith("😄");
    }

    [Fact]
    public void Набор_подключается_во_вьюер_одной_строкой()
    {
        WpfHost.Invoke(() =>
        {
            var viewer = new MarkdownViewer { Markdown = "Hi @ivan :wave:" };
            viewer.Extensions.Add(new ChatSet());
            var window = WpfHost.Show(viewer);

            viewer.TextMap.Text.Should().Be("Hi " + Selection.MdTextMap.ObjectMark + " 👋");
            window.Close();
        });
    }

    private static IReadOnlyList<BlockVm> Parse(string markdown, MdExtension extension)
        => ModelBuilder.Parse(markdown, ModelDump.Pipeline(extension), [extension]);

    private sealed class Names : IMdLinkResolver
    {
        public bool PageExists(string page) => page == "Known";

        public string? DisplayName(string user) => user == "ivan" ? "Иван" : null;
    }
}
