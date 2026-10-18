namespace Beresta.Markdown.Wpf.Kit;

using Selection;
using ViewModels;

/// <summary>Карточка свойств: front matter строками «ключ — значение».</summary>
public sealed record PropertiesVm(IReadOnlyList<PropertyVm> Items) : BlockVm, IMdTextSource
{
    public void WriteText(MdTextWriter writer)
    {
        for (var i = 0; i < Items.Count; i++)
        {
            if (i > 0)
            {
                writer.Text("\n");
            }

            writer.Inlines(Items[i].Key);
            writer.Text("\t");
            writer.Inlines(Items[i].Value);
        }
    }
}
