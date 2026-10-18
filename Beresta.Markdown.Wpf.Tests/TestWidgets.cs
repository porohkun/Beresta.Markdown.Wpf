namespace Beresta.Markdown.Wpf.Tests;

using System.Windows;
using System.Windows.Markup;
using Markdig;
using Markdig.Extensions.CustomContainers;
using Markdig.Extensions.TaskLists;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Selection;
using ViewModels;

/// <summary>Виджеты и расширения для тестов: заметка, диаграмма, галочка, упоминание.</summary>
internal static class TestWidgets
{
    private const string Namespaces =
        "xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' "
        + "xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' "
        + "xmlns:mdv='clr-namespace:Beresta.Markdown.Wpf;assembly=Beresta.Markdown.Wpf' "
        + "xmlns:p='clr-namespace:Beresta.Markdown.Wpf.Presenters;assembly=Beresta.Markdown.Wpf' "
        + "xmlns:e='clr-namespace:Beresta.Markdown.Wpf.Extensions;assembly=Beresta.Markdown.Wpf'";

    /// <summary><c>:::note Заголовок</c> — заметка со своей разметкой внутри.</summary>
    public static LambdaExtension Note() => new()
    {
        Block = (block, context) => block is CustomContainer { Info: "note" } container
            ? new NoteVm(container.Arguments ?? string.Empty, context.ConvertChildren(container))
            : null,
        Template = item => item is NoteVm
            ? Template("<Border Tag='note'><p:MdBlocksPresenter Blocks='{Binding Blocks}' /></Border>")
            : null
    };

    /// <summary>Блок кода с языком <c>diagram</c> вместо встроенного блока кода.</summary>
    public static LambdaExtension Diagram() => new()
    {
        Block = (block, context) => block is FencedCodeBlock { Info: "diagram" } code
            ? new DiagramVm(context.GetSource(code))
            : null
    };

    /// <summary>Отметка задачи — строчный виджет из узла, который Markdig уже знает.</summary>
    public static LambdaExtension Check() => new()
    {
        Inline = (inline, _) => inline is TaskList task ? new CheckVm(task.Checked) : null
    };

    /// <summary><c>@имя</c> — свой синтаксис через свою настройку разбора.</summary>
    public static LambdaExtension Mention() => new()
    {
        Setup = pipeline => pipeline.Extensions.AddIfNotAlready(new MentionMarkdig()),
        Inline = (inline, _) => inline is MentionInline mention ? new MentionVm(mention.Name) : null,
        Template = item => item is MentionVm
            ? Template("<TextBlock Tag='mention' Text='{Binding Name}' />")
            : null
    };

    public static DataTemplate Template(string content)
    {
        return (DataTemplate)XamlReader.Parse($"<DataTemplate {Namespaces}>{content}</DataTemplate>");
    }

    public sealed record NoteVm(string Title, IReadOnlyList<BlockVm> Blocks) : BlockVm;

    public sealed record DiagramVm(string Code) : BlockVm;

    public sealed record CheckVm(bool Checked) : InlineVm;

    public sealed record MentionVm(string Name) : InlineVm, IMdCopyText
    {
        public string CopyText => "@" + Name;
    }

    private sealed class MentionInline : LeafInline
    {
        public required string Name { get; init; }
    }

    private sealed class MentionParser : InlineParser
    {
        public MentionParser()
        {
            OpeningCharacters = ['@'];
        }

        public override bool Match(InlineProcessor processor, ref StringSlice slice)
        {
            var start = slice.Start;
            var length = 0;
            while (char.IsLetterOrDigit(slice.PeekChar(length + 1)))
            {
                length++;
            }

            if (length == 0)
            {
                return false;
            }

            var position = processor.GetSourcePosition(start, out var line, out var column);
            processor.Inline = new MentionInline
            {
                Name = slice.Text.Substring(start + 1, length),
                Span = new(position, position + length),
                Line = line,
                Column = column
            };

            slice.Start += length + 1;
            return true;
        }
    }

    private sealed class MentionMarkdig : IMarkdownExtension
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
}