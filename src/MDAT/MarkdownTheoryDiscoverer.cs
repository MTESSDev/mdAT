using LoxSmoke.DocXml;
using Markdig;
using Markdig.Syntax;
using MDAT.Resolver;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using Xunit.Abstractions;
using Xunit.Sdk;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.System.Text.Json;

namespace MDAT
{
    public sealed class MarkdownTheoryDiscoverer : IXunitTestCaseDiscoverer
    {
        readonly IMessageSink _diagnostics;

        public MarkdownTheoryDiscoverer(IMessageSink diagnosticMessageSink)
        {
            _diagnostics = diagnosticMessageSink;
        }

        public IEnumerable<IXunitTestCase> Discover(
            ITestFrameworkDiscoveryOptions discoveryOptions,
            ITestMethod testMethod,
            IAttributeInfo factAttribute)
        {

            _diagnostics.OnMessage(new DiagnosticMessage("Hello from discoverer!"));

            var filePath = factAttribute.GetConstructorArguments().FirstOrDefault() as string
                           ?? throw new InvalidOperationException("filePath manquant sur MarkdownTheoryAttribute.");

            var resolvedPath = ResolvePath(testMethod, filePath);

            if (!File.Exists(resolvedPath))
            {
                CreateMarkdownSkeletonIfMissing(testMethod, resolvedPath);
                yield break;
            }

            foreach (var row in MarkdownCaseLoader.Load(testMethod, resolvedPath))
            {
                var displayName = BuildDisplayName(testMethod, resolvedPath, row.Label);

                yield return new MarkdownTestCase(
                    _diagnostics,
                    discoveryOptions.MethodDisplayOrDefault(),
                    discoveryOptions.MethodDisplayOptionsOrDefault(),
                    testMethod,
                    displayName,
                    row.Arguments);
            }
        }

        static string ResolvePath(ITestMethod testMethod, string filePath)
        {
            var methodNameKebab = testMethod.Method.ToRuntimeMethod().Name.ToKebabCase().ToLowerInvariant();
            var candidate = filePath.Replace("{method}", methodNameKebab);
            return Extensions.GetCurrentPath(candidate, false);
        }

        static string BuildDisplayName(ITestMethod testMethod, string path, string label)
        {
            var method = $"{testMethod.TestClass.Class.Name}.{testMethod.Method.Name}";
            return $"{method}@{path}{label}";
        }

        static void CreateMarkdownSkeletonIfMissing(ITestMethod testMethod, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            var runtimeMethod = testMethod.Method.ToRuntimeMethod();
            var asm = runtimeMethod.DeclaringType!.Assembly;

            var xmlFilePath = Path.ChangeExtension(asm.Location, ".xml");

            // Lire commentaires XML si dispo
            MethodComments? methodComments = null;
            if (File.Exists(xmlFilePath))
            {
                var reader = new DocXmlReader(xmlFilePath);
                methodComments = reader.GetMethodComments(runtimeMethod);
            }

            // Construit le bloc YAML "exemple" à partir des paramètres du test
            var sb = new StringBuilder();
            foreach (var p in runtimeMethod.GetParameters())
            {
                var pDoc = methodComments?.Parameters.FirstOrDefault(e => e.Name == p.Name);

                var docLine = (pDoc is { } && pDoc.Value.Text is { })
                    ? $"# {pDoc.Value.Text}\n"
                    : string.Empty;

                sb.Append(docLine);
                sb.Append(p.Name);
                sb.Append(":\n");
                sb.Append(DescribeTypeOfObject(p.ParameterType, "  "));
            }

            // Summary en quote markdown si présent
            var summary = (methodComments is { } && !string.IsNullOrWhiteSpace(methodComments.Summary))
                            ? $"\n\n> {methodComments.Summary.ReplaceLineEndings("\\\n")}"
                            : string.Empty;

            // Contenu final
            var content = $"# {runtimeMethod.Name}{summary}\n\n## Case 1\n\nDescription\n\n``````yaml\n{sb}``````";

            File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        // ---------- helpers ----------

        static string? DescribeTypeOfObject(Type type, string indent, int depth = 0)
        {
            if (depth >= 10) return string.Empty; // garde-fou récursion
            depth++;

            string? obj = null;

            // Type "modèle" (classe non System.*)
            if (type.IsClass && type.FullName is { } fn && !fn.StartsWith("System.", StringComparison.Ordinal))
            {
                foreach (var pi in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var def = GetDefaultValueForProperty(pi);
                    var inner = DescribeTypeOfObject(pi.PropertyType, indent + "  ", depth);
                    obj += $"{indent}{pi.Name}: {(string.IsNullOrEmpty(inner) ? GetDefaultValue(def) : "\n" + inner)}";
                }
            }
            // IEnumerable<T> simple
            else if (IsGenericEnumerable(type, out var elemType))
            {
                var props = elemType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var first = true;
                foreach (var pi in props)
                {
                    var def = GetDefaultValueForProperty(pi);
                    var inner = DescribeTypeOfObject(pi.PropertyType, indent + "    ", depth);
                    obj += $"{indent}{(first ? "- " : "  ")}{pi.Name}: {(string.IsNullOrEmpty(inner) ? GetDefaultValue(def) : "\n" + inner)}";
                    first = false;
                }
            }

            return obj;
        }

        static bool IsGenericEnumerable(Type t, out Type elementType)
        {
            elementType = typeof(object);
            if (t == typeof(string)) return false;

            var ienum = t.GetInterfaces()
                         .Concat(new[] { t })
                         .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            if (ienum is null) return false;

            elementType = ienum.GetGenericArguments()[0];
            return true;
        }

        static string GetDefaultValue(object? def)
        {
            return def is { } ? new Serializer().Serialize(def).ReplaceLineEndings("\n") : "null\n";
        }

        static object? GetDefaultValueForProperty(PropertyInfo property)
        {
            var defaultAttr = property.GetCustomAttribute(typeof(DefaultValueAttribute));
            if (defaultAttr != null)
                return (defaultAttr as DefaultValueAttribute)?.Value;

            var propertyType = property.PropertyType;
            return propertyType.IsValueType ? Activator.CreateInstance(propertyType) : null;
        }
    }

    public sealed class MarkdownDataRow
    {
        public MarkdownDataRow(object?[] arguments, string label)
        {
            Arguments = arguments;
            Label = label;
        }
        public object?[] Arguments { get; }
        public string Label { get; }
    }

    // ---- Charge et parse le Markdown/YAML ----
    public static class MarkdownCaseLoader
    {
        public static IEnumerable<MarkdownDataRow> Load(ITestMethod testMethod, string path)
        {
            var rows = LoadMarkdownFile(testMethod, path).ToList();
            foreach (var (args, label) in rows)
                yield return new MarkdownDataRow(args, label);
        }

        private static IEnumerable<(object?[] Args, string Label)> LoadMarkdownFile(ITestMethod testMethod, string path)
        {
            var fileData = File.ReadAllText(path);
            var mdFile = Markdown.Parse(fileData);

            // On mémorise jusqu’à 6 niveaux de titres (H1..H6)
            string?[] headings = new string?[6];

            var normal = new List<(object?[] Args, string Label)>();
            var selected = new List<(object?[] Args, string Label)>();

            foreach (var node in mdFile)
            {
                if (node is HeadingBlock h)
                {
                    SetHeadings(headings, h);
                }
                else if (node is FencedCodeBlock fbc && IsYamlFence(fbc))
                {
                    var doc = string.Join("\n", fbc.Lines);
                    if (string.IsNullOrWhiteSpace(doc))
                        continue;

                    // flags "skipped"/"selected" dans la signature du fenced block
                    var hasArgs = fbc.Arguments is { };
                    if (hasArgs && fbc.Arguments.Contains("skipped", StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    // Parse YAML -> object[]
                    var runtimeMethod = testMethod.Method.ToRuntimeMethod();
                    var args = ExtractTest(runtimeMethod, doc, Path.GetDirectoryName(path)!);

                    // Label à partir des headings courants
                    var label = "_" + string.Join("_", headings.Where(s => !string.IsNullOrWhiteSpace(s)));

                    if (hasArgs && fbc.Arguments.Contains("selected", StringComparison.InvariantCultureIgnoreCase))
                        selected.Add((args, label));
                    else
                        normal.Add((args, label));
                }
            }

            return (selected.Any() ? selected : normal);
        }

        private static bool IsYamlFence(FencedCodeBlock fbc)
        {
            if (fbc.ClosingFencedCharCount != 6) return false;
            var info = fbc.Info?.ToLowerInvariant();
            return info == "yaml" || info == "yml";
        }

        private static void SetHeadings(string?[] heading, HeadingBlock add)
        {
            heading[add.Level - 1] = add.Inline?.FirstOrDefault()?.ToString();
            for (int i = add.Level; i < heading.Length; i++)
                heading[i] = null;
        }

        private static object?[] ExtractTest(MethodInfo runtimeMethod, string doc, string directoryName)
        {
            var resolver = new MDATYamlTypeResolver(runtimeMethod);
            var deserializer = NewDeserializer(resolver, directoryName);

            try
            {
                var dict = deserializer.Deserialize<Dictionary<string, object?>>(doc);
                if (dict is null) return Array.Empty<object?>();
                return dict.Values.ToArray();
            }
            catch (YamlException ex)
            {
                // Affiche le param fautif si possible
                var pos = Math.Max(1, resolver.Position);
                var pars = runtimeMethod.GetParameters();
                var badParam = (pos - 1) >= 0 && (pos - 1) < pars.Length ? pars[pos - 1].Name : "?";
                throw new XunitException(
                    $"Method '{runtimeMethod.Name}' exception on '{badParam}' parameter.\n{ex.Message}\n{ex.InnerException?.Message}");
            }
        }

        private static IDeserializer NewDeserializer(INodeTypeResolver resolver, string directoryName)
        {
            var builder = new DeserializerBuilder()
                .WithTypeConverter(new ByteArayConverter(), e => e.OnBottom())
                .WithTypeConverter(new SystemTextJsonYamlTypeConverter())
                .WithTypeInspector(x => new SystemTextJsonTypeInspector(x))
                .WithNodeTypeResolver(resolver)
                .WithNodeDeserializer(new KeyValuePairNodeDeserializer())
                .IgnoreUnmatchedProperties()
                .WithAttemptingUnquotedStringTypeDeserialization()
                .WithTagMapping(MdatConstants.IncludeTag, typeof(IncludeRef));

            var includeOptions = new YamlIncludeNodeDeserializerOptions
            {
                DirectoryName = directoryName
            };
            var includeNode = new YamlIncludeNodeDeserializer(includeOptions);

            builder.WithNodeDeserializer(includeNode, o => o.OnTop());
            return builder.Build();
        }
    }
}
