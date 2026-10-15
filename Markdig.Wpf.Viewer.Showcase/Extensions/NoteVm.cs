namespace MarkdigWpfViewer.Showcase.Extensions;

using Selection;
using ViewModels;

/// <summary>Заметка: заголовок и вложенные блоки; выделяется и копируется вместе с ними.</summary>
public sealed record NoteVm(string Title, IReadOnlyList<BlockVm> Blocks) : BlockVm, IMdTextSource
{
    public void WriteText(MdTextWriter writer)
    {
        writer.Text(Title);
        writer.Text("\n");
        writer.Blocks(Blocks);
    }
}