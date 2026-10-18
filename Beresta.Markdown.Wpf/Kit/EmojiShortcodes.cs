namespace Beresta.Markdown.Wpf.Kit;

using Extensions;
using Markdig;

/// <summary>Emoji shortcodes <c>:smile:</c> as emoji; smileys like <c>:)</c> stay text.</summary>
public sealed class EmojiShortcodes : MdExtension
{
    /// <inheritdoc/>
    public override void SetupPipeline(MarkdownPipelineBuilder pipeline)
        => pipeline.UseEmojiAndSmiley(enableSmileys: false);
}
