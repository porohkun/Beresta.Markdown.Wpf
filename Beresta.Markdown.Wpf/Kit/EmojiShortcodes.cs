namespace Beresta.Markdown.Wpf.Kit;

using Extensions;
using Markdig;

/// <summary>Эмодзи-шорткоды <c>:smile:</c> — знаком эмодзи; смайлики вида <c>:)</c> остаются текстом.</summary>
public sealed class EmojiShortcodes : MdExtension
{
    public override void SetupPipeline(MarkdownPipelineBuilder pipeline)
        => pipeline.UseEmojiAndSmiley(enableSmileys: false);
}
