using System.Collections;
using System.Reflection;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Class2Json.Converter;

public static class JsonConverter
{
    public static string ConvertClass(string sourceCode, bool useCamelCase = true)
    {
        if (string.IsNullOrWhiteSpace(sourceCode))
            return string.Empty;

        var compiledAssembly = CompileSourceCode(sourceCode);
        var classTypes = GetOrderedClassTypes(compiledAssembly);

        var jsonProperties = new Dictionary<string, object>();
        var addedProperties = new HashSet<string>();

        foreach (var classType in classTypes)
        {
            AddClassProperties(classType, jsonProperties, addedProperties, useCamelCase);
        }

        return JsonSerializer.Serialize(jsonProperties);
    }

    private static void AddClassProperties(Type classType, Dictionary<string, object> jsonProperties,
        HashSet<string> addedProperties, bool useCamelCase)
    {
        foreach (var property in classType.GetProperties())
        {
            var jsonKey = useCamelCase ? property.Name.ToCamelCase() : property.Name;
            if (!addedProperties.Contains(jsonKey))
            {
                if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
                {
                    if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
                    {
                        jsonProperties[jsonKey] = GetEnumerableDefaultValue(property.PropertyType);
                    }
                    else
                    {
                        var nestedProperties = new Dictionary<string, object>();
                        AddClassProperties(property.PropertyType, nestedProperties, addedProperties, useCamelCase);
                        jsonProperties[jsonKey] = nestedProperties;
                    }
                }
                else
                {
                    jsonProperties[jsonKey] = GetDefaultValue(property.PropertyType);
                }

                addedProperties.Add(jsonKey);
            }
        }
    }

    private static object GetEnumerableDefaultValue(Type type)
    {
        if (type.IsArray)
        {
            return Array.CreateInstance(type.GetElementType(), 0);
        }

        if (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type))
        {
            var elementType = type.GetGenericArguments()[0];
            var listType = typeof(List<>).MakeGenericType(elementType);
            return Activator.CreateInstance(listType);
        }

        return null;
    }

    private static Assembly CompileSourceCode(string sourceCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>();

        var compilation = CSharpCompilation.Create("DynamicAssembly")
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddReferences(references)
            .AddSyntaxTrees(syntaxTree);

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            var errors = string.Join(Environment.NewLine, result.Diagnostics
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Select(diagnostic => diagnostic.ToString()));
            throw new InvalidOperationException($"Compilation failed: {errors}");
        }

        ms.Seek(0, SeekOrigin.Begin);
        return Assembly.Load(ms.ToArray());
    }

    private static List<Type> GetOrderedClassTypes(Assembly assembly)
    {
        var classTypes = assembly.GetTypes().Where(t => t.IsClass).ToList();
        var orderedClassTypes = new List<Type>();

        while (classTypes.Count > 0)
        {
            var rootClass = classTypes.FirstOrDefault(ct =>
                !classTypes.Any(t => t.GetProperties().Any(p => p.PropertyType == ct)));
            
            if (rootClass != null)
            {
                orderedClassTypes.Add(rootClass);
                classTypes.Remove(rootClass);
            }
            else
            {
                orderedClassTypes.AddRange(classTypes);
                break;
            }
        }

        return orderedClassTypes;
    }

    private static object GetDefaultValue(Type type)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }

        return type == typeof(string) ? string.Empty : null;
    }
}