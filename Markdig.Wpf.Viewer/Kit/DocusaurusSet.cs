namespace MarkdigWpfViewer.Kit;

/// <summary>Набор Docusaurus и VuePress: контейнеры <c>:::вид</c>.</summary>
public sealed class DocusaurusSet : KitSet
{
    public DocusaurusSet()
    {
        Part(KitParts.CalloutContainers, new CalloutContainers());
    }
}
