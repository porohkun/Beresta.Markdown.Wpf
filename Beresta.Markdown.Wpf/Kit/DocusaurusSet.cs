namespace Beresta.Markdown.Wpf.Kit;

/// <summary>The Docusaurus and VuePress set: containers <c>:::kind</c>.</summary>
public sealed class DocusaurusSet : KitSet
{
    /// <summary>Creates the set with all its parts.</summary>
    public DocusaurusSet()
    {
        Part(KitParts.CalloutContainers, new CalloutContainers());
    }
}
