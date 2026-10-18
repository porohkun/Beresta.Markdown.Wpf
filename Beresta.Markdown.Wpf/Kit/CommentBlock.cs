namespace Beresta.Markdown.Wpf.Kit;

using Markdig.Parsers;
using Markdig.Syntax;

/// <summary>Комментарий <c>%%</c> блоком на несколько строк.</summary>
internal sealed class CommentBlock(BlockParser parser) : LeafBlock(parser);
