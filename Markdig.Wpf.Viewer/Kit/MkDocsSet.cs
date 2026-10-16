namespace MarkdigWpfViewer.Kit;

/// <summary>Набор MkDocs: предупреждения <c>!!!</c> и <c>???</c>.</summary>
public sealed class MkDocsSet : KitSet
{
    public MkDocsSet()
    {
        Part(KitParts.MkDocsAdmonitions, new MkDocsAdmonitions());
    }
}
