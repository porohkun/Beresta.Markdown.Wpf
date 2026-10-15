namespace MarkdigWpfViewer.Showcase.Extensions;

using Markdig;
using Markdig.Renderers;

/// <summary>Расширение Markdig, которое подключает <see cref="MentionParser"/>.</summary>
public sealed class MentionMarkdigExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        if (!pipeline.InlineParsers.Contains<MentionParser>())
        {
            pipeline.InlineParsers.Insert(0, new MentionParser());
        }
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
    }
}