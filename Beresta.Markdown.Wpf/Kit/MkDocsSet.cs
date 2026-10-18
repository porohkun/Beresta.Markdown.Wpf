namespace Beresta.Markdown.Wpf.Kit;

/// <summary>The MkDocs set: admonitions <c>!!!</c> and <c>???</c>.</summary>
public sealed class MkDocsSet : KitSet
{
    /// <summary>Creates the set with all its parts.</summary>
    public MkDocsSet()
    {
        Part(KitParts.MkDocsAdmonitions, new MkDocsAdmonitions());
    }
}
