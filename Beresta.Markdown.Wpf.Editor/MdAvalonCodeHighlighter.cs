namespace Beresta.Markdown.Wpf.Editor;

using Extensions;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;

/// <summary>
/// Highlights code blocks with the highlighting definitions of AvalonEdit: C#, JavaScript, HTML, XML,
/// CSS, C++, Java, PHP, PowerShell, Python, SQL, VB, JSON, diff and Markdown. Safe on a background thread.
/// </summary>
public sealed class MdAvalonCodeHighlighter : IMdCodeHighlighter, IMdBackgroundSafe
{
    private static readonly Dictionary<string, string> Definitions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cs"] = "C#",
        ["csharp"] = "C#",
        ["c#"] = "C#",
        ["js"] = "JavaScript",
        ["javascript"] = "JavaScript",
        ["jsx"] = "JavaScript",
        ["mjs"] = "JavaScript",
        ["ts"] = "JavaScript",
        ["typescript"] = "JavaScript",
        ["tsx"] = "JavaScript",
        ["html"] = "HTML",
        ["htm"] = "HTML",
        ["xhtml"] = "HTML",
        ["xml"] = "XML",
        ["xaml"] = "XML",
        ["csproj"] = "XML",
        ["svg"] = "XML",
        ["xsd"] = "XML",
        ["css"] = "CSS",
        ["c"] = "C++",
        ["h"] = "C++",
        ["cpp"] = "C++",
        ["c++"] = "C++",
        ["cc"] = "C++",
        ["hpp"] = "C++",
        ["java"] = "Java",
        ["diff"] = "Patch",
        ["patch"] = "Patch",
        ["ps1"] = "PowerShell",
        ["powershell"] = "PowerShell",
        ["pwsh"] = "PowerShell",
        ["php"] = "PHP",
        ["py"] = "Python",
        ["python"] = "Python",
        ["sql"] = "TSQL",
        ["tsql"] = "TSQL",
        ["vb"] = "VB",
        ["vbnet"] = "VB",
        ["json"] = "Json",
        ["jsonc"] = "Json",
        ["md"] = "MarkDown",
        ["markdown"] = "MarkDown"
    };

    /// <summary>The highlighter for all viewers and editors.</summary>
    public static MdAvalonCodeHighlighter Instance { get; } = new();

    /// <inheritdoc/>
    public IReadOnlyList<MdCodeSpan>? Highlight(string language, string code)
    {
        var manager = HighlightingManager.Instance;
        var definition = Definitions.TryGetValue(language, out var name) ? manager.GetDefinition(name) : manager.GetDefinitionByExtension("." + language);
        if (definition is null)
        {
            return null;
        }

        var document = new TextDocument(code);
        var highlighter = new DocumentHighlighter(document, definition);
        var spans = new List<MdCodeSpan>();
        for (var number = 1; number <= document.LineCount; number++)
        {
            foreach (var section in highlighter.HighlightLine(number).Sections)
            {
                if (section.Length > 0 && LookOf(definition.Name, section.Color?.Name) is { } look)
                {
                    spans.Add(new(section.Offset, section.Length, look));
                }
            }
        }

        return spans;
    }

    /// <summary>Вид куска по имени цвета определения AvalonEdit; имена у языков свои, виды — общие.</summary>
    private static string? LookOf(string definition, string? name) => name switch
    {
        null => null,
        "AddedText" => "Inserted",
        "RemovedText" => "Deleted",
        "TypeKeywords" when definition == "C#" => "Keyword",
        "Code" => "String",
        "Link" or "Image" => "Attribute",
        _ when Has(name, "Heading") => "Keyword",
        _ when Has(name, "Comment", "DocTag", "Header", "Position", "FileName", "BlockQuote") => "Comment",
        _ when Has(name, "String", "Char", "Regex", "CData", "AttributeValue", "DateLiteral") => "String",
        _ when Has(name, "Number", "Digit") => "Number",
        _ when Has(name, "Preprocessor", "DocType", "XmlDeclaration") => "Preprocessor",
        _ when Has(name, "Tag", "Selector") => "Tag",
        _ when Has(name, "Attribute", "Property", "FieldName") => "Attribute",
        _ when Has(name, "Type", "Void", "Class", "Intrinsic") => "Type",
        _ when Has(name, "Keyword") => "Keyword",
        _ when Has(name, "Punctuation", "CurlyBraces", "Colon", "Slash", "Assignment") => "Punctuation",
        _ when Has(name, "Operator", "Modifier", "Visibility", "TrueFalse", "Bool", "Null", "Literal", "Constant", "This", "Statement", "Namespace", "Package", "GetSetAddRemove", "Friend", "Exception", "ControlFlow", "GlobalFunction", "Entit") => "Keyword",
        _ => null
    };

    private static bool Has(string name, params string[] parts) => parts.Any(part => name.Contains(part, StringComparison.OrdinalIgnoreCase));
}
