namespace Beresta.Markdown.Wpf.Kit;

using Selection;
using ViewModels;

/// <summary>The properties card: front matter as «key — value» rows.</summary>
public sealed record PropertiesVm(IReadOnlyList<PropertyVm> Items) : BlockVm, IMdTextSource
{
    /// <inheritdoc/>
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
