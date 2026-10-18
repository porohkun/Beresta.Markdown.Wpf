namespace Beresta.Markdown.Wpf.Tests;

using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

/// <summary>Доки публичного API: английские в коде и русский перевод отдельным файлом.</summary>
public sealed class DocumentationTests
{
    private static readonly string EnglishFile = Path.Combine(AppContext.BaseDirectory, "Beresta.Markdown.Wpf.xml");
    private static readonly string RussianFile = Path.Combine(AppContext.BaseDirectory, "Docs", "Beresta.Markdown.Wpf.ru.xml");

    [Fact]
    public void Русский_файл_переводит_ровно_публичный_API()
    {
        var visible = PublicIds(typeof(MarkdownViewer).Assembly);
        var expected = Members(EnglishFile).Keys.Where(visible.Contains);

        Members(RussianFile).Keys.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Перевод_повторяет_устройство_английской_доки()
    {
        var english = Members(EnglishFile);
        var mismatches = Members(RussianFile)
            .Where(pair => english.TryGetValue(pair.Key, out var original) && Shape(original) != Shape(pair.Value))
            .Select(pair => $"{pair.Key}: {Shape(english[pair.Key])} | {Shape(pair.Value)}")
            .ToList();

        mismatches.Should().BeEmpty();
    }

    [Fact]
    public void У_каждого_публичного_члена_есть_описание()
    {
        var visible = PublicIds(typeof(MarkdownViewer).Assembly);
        var empty = Members(EnglishFile)
            .Where(pair => visible.Contains(pair.Key))
            .Concat(Members(RussianFile))
            .Where(pair => pair.Value.Element("inheritdoc") is null && string.IsNullOrWhiteSpace(pair.Value.Element("summary")?.Value))
            .Select(pair => pair.Key)
            .ToList();

        empty.Should().BeEmpty();
    }

    private static Dictionary<string, XElement> Members(string path)
    {
        return XDocument.Load(path)
            .Descendants("member")
            .ToDictionary(member => (string)member.Attribute("name")!);
    }

    private static string Shape(XElement member)
    {
        return string.Join(
            "; ",
            member.Elements().Select(element => element.Name.LocalName + string.Concat(element.Attributes().Select(attribute => $" {attribute.Name}={attribute.Value}"))));
    }

    private static HashSet<string> PublicIds(Assembly assembly)
    {
        const BindingFlags declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        var ids = new HashSet<string>();
        foreach (var type in assembly.GetTypes().Where(type => Visible(type) && Browsable(type)))
        {
            var name = TypeName(type);
            ids.Add("T:" + name);
            foreach (var member in type.GetMembers(declared).Where(Browsable))
            {
                var id = member switch
                {
                    ConstructorInfo constructor when Visible(constructor) => $"M:{name}.#ctor{Parameters(constructor.GetParameters())}",
                    MethodInfo method when Visible(method) => $"M:{name}.{method.Name}{Arity(method)}{Parameters(method.GetParameters())}{Conversion(method)}",
                    PropertyInfo property when property.GetAccessors(true).Any(Visible) => $"P:{name}.{property.Name}{Parameters(property.GetIndexParameters())}",
                    FieldInfo field when field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly => $"F:{name}.{field.Name}",
                    EventInfo @event when Visible(@event.AddMethod!) => $"E:{name}.{@event.Name}",
                    _ => null
                };
                if (id is not null)
                {
                    ids.Add(id);
                }
            }
        }

        return ids;
    }

    private static bool Visible(Type type)
    {
        return type.IsNested
            ? (type.IsNestedPublic || type.IsNestedFamily || type.IsNestedFamORAssem) && Visible(type.DeclaringType!)
            : type.IsPublic;
    }

    private static bool Visible(MethodBase method)
    {
        return method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly;
    }

    private static bool Browsable(MemberInfo member)
    {
        return member.GetCustomAttribute<EditorBrowsableAttribute>()?.State != EditorBrowsableState.Never;
    }

    private static string Arity(MethodInfo method)
    {
        return method.IsGenericMethodDefinition ? "``" + method.GetGenericArguments().Length : "";
    }

    private static string Parameters(ParameterInfo[] parameters)
    {
        return parameters.Length == 0 ? "" : "(" + string.Join(",", parameters.Select(parameter => TypeId(parameter.ParameterType))) + ")";
    }

    private static string Conversion(MethodInfo method)
    {
        return method.Name is "op_Implicit" or "op_Explicit" ? "~" + TypeId(method.ReturnType) : "";
    }

    private static string TypeName(Type type)
    {
        return type.FullName!.Replace('+', '.');
    }

    private static string TypeId(Type type)
    {
        if (type.IsByRef)
        {
            return TypeId(type.GetElementType()!) + "@";
        }

        if (type.IsArray)
        {
            return TypeId(type.GetElementType()!) + "[]";
        }

        if (type.IsGenericParameter)
        {
            return (type.DeclaringMethod is null ? "`" : "``") + type.GenericParameterPosition;
        }

        if (type.IsGenericType)
        {
            var definition = TypeName(type.GetGenericTypeDefinition());
            return definition[..definition.IndexOf('`')] + "{" + string.Join(",", type.GetGenericArguments().Select(TypeId)) + "}";
        }

        return TypeName(type);
    }
}
