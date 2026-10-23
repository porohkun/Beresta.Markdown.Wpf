namespace Beresta.Markdown.Wpf.Tests;

using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

/// <summary>Доки публичного API обоих пакетов: английские в коде и русский перевод отдельным файлом.</summary>
public sealed class DocumentationTests
{
    public static TheoryData<Type> Packages => new() { typeof(MarkdownViewer), typeof(Editor.MarkdownEditor) };

    [Theory]
    [MemberData(nameof(Packages))]
    public void Русский_файл_переводит_ровно_публичный_API(Type package)
    {
        var visible = PublicIds(package.Assembly);
        var expected = Members(EnglishFile(package)).Keys.Where(visible.Contains);

        Members(RussianFile(package)).Keys.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [MemberData(nameof(Packages))]
    public void Перевод_повторяет_устройство_английской_доки(Type package)
    {
        var english = Members(EnglishFile(package));
        var mismatches = Members(RussianFile(package))
            .Where(pair => english.TryGetValue(pair.Key, out var original) && Shape(original) != Shape(pair.Value))
            .Select(pair => $"{pair.Key}: {Shape(english[pair.Key])} | {Shape(pair.Value)}")
            .ToList();

        mismatches.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(Packages))]
    public void У_каждого_публичного_члена_есть_описание(Type package)
    {
        var visible = PublicIds(package.Assembly);
        var empty = Members(EnglishFile(package))
            .Where(pair => visible.Contains(pair.Key))
            .Concat(Members(RussianFile(package)))
            .Where(pair => pair.Value.Element("inheritdoc") is null && string.IsNullOrWhiteSpace(pair.Value.Element("summary")?.Value))
            .Select(pair => pair.Key)
            .ToList();

        empty.Should().BeEmpty();
    }

    private static string EnglishFile(Type package)
        => Path.Combine(AppContext.BaseDirectory, package.Assembly.GetName().Name + ".xml");

    private static string RussianFile(Type package)
        => Path.Combine(AppContext.BaseDirectory, "Docs", package.Assembly.GetName().Name + ".ru.xml");

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
