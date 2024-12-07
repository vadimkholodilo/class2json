using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Class2Json.Converter
{
    public static class JsonConverter
    {
        public static string ConvertClass(string sourceCode, bool useCamelCase = true)
        {
            if (string.IsNullOrWhiteSpace(sourceCode))
                return string.Empty;

            var compiledAssembly = CompileSourceCode(sourceCode);
            var classTypes = compiledAssembly.GetTypes().Where(t => t.IsClass).ToList();

            var jsonProperties = new Dictionary<string, object>();

            foreach (var classType in classTypes)
            {
                var classInstance = Activator.CreateInstance(classType);
                foreach (var property in classType.GetProperties())
                {
                    var jsonKey = useCamelCase ? ToCamelCase(property.Name) : property.Name;
                    if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
                    {
                        jsonProperties[jsonKey] = GetNestedClassProperties(property.PropertyType);
                    }
                    else
                    {
                        jsonProperties[jsonKey] = GetDefaultValue(property.PropertyType);
                    }
                }
            }

            return JsonSerializer.Serialize(jsonProperties);
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

        private static string ToCamelCase(string str)
        {
            if (string.IsNullOrEmpty(str) || !char.IsUpper(str[0]))
                return str;

            var chars = str.ToCharArray();
            chars[0] = char.ToLower(chars[0]);
            return new string(chars);
        }

        private static object GetDefaultValue(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }

            return type == typeof(string) ? string.Empty : null;
        }

        private static Dictionary<string, object> GetNestedClassProperties(Type type)
        {
            var nestedProperties = type.GetProperties();
            var nestedJsonProperties = new Dictionary<string, object>();

            foreach (var property in nestedProperties)
            {
                var jsonKey = ToCamelCase(property.Name);
                nestedJsonProperties[jsonKey] = GetDefaultValue(property.PropertyType);
            }

            return nestedJsonProperties;
        }
    }
}