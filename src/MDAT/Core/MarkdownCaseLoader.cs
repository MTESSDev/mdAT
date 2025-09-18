// MDAT.Core
using LoxSmoke.DocXml;
using Markdig;
using Markdig.Syntax;
using MDAT.Resolver;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.System.Text.Json;

namespace MDAT
{
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

    public static class MarkdownCaseLoader
    {
        public static IEnumerable<MarkdownDataRow> Load(MethodInfo runtimeMethod, string path)
        {
            var rows = LoadMarkdownFile(runtimeMethod, path).ToList();
            foreach (var (args, label) in rows)
                yield return new MarkdownDataRow(args, label);
        }

        public static string ResolvePath(MethodInfo method, string filePath)
        {
            var methodNameKebab = ToKebabCase(method.Name).ToLowerInvariant();
            var candidate = filePath.Replace("{method}", methodNameKebab);

            // Base: répertoire de l’assembly contenant la classe de test
            var baseDir = Path.GetDirectoryName(method.DeclaringType!.Assembly.Location)!;
            var combined = Path.IsPathRooted(candidate) ? candidate : Path.GetFullPath(Path.Combine(baseDir, candidate));
            return combined;
        }

        public static void CreateMarkdownSkeletonIfMissing(MethodInfo runtimeMethod, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            var asm = runtimeMethod.DeclaringType!.Assembly;
            var xmlFilePath = Path.ChangeExtension(asm.Location, ".xml");

            MethodComments? methodComments = null;
            if (File.Exists(xmlFilePath))
            {
                var reader = new DocXmlReader(xmlFilePath);
                methodComments = reader.GetMethodComments(runtimeMethod);
            }

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

            var summary = (methodComments is { } && !string.IsNullOrWhiteSpace(methodComments.Summary))
                            ? $"\n\n> {methodComments.Summary.ReplaceLineEndings("\\\n")}"
                            : string.Empty;

            var content = $"# {runtimeMethod.Name}{summary}\n\n## Case 1\n\nDescription\n\n``````yaml\n{sb}``````";
            File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        // ---------- internes ----------

        private static IEnumerable<(object?[] Args, string Label)> LoadMarkdownFile(MethodInfo runtimeMethod, string path)
        {
            var fileData = File.ReadAllText(path);
            var mdFile = Markdown.Parse(fileData);

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

                    // flags "skipped"/"selected"
                    var hasArgs = fbc.Arguments is { };
                    if (hasArgs && fbc.Arguments.Contains("skipped", StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    var args = ExtractTest(runtimeMethod, doc, Path.GetDirectoryName(path)!);
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
            // Tu as déjà MDATYamlTypeResolver dans ton projet -> on le réutilise
            var resolver = new MDAT.Resolver.MDATYamlTypeResolver(runtimeMethod);

            var builder = new DeserializerBuilder()
                // Si tu as un ByteArray converter custom, garde-le; sinon commente la ligne suivante.
                // .WithTypeConverter(new ByteArayConverter(), e => e.OnBottom())
                .WithTypeConverter(new SystemTextJsonYamlTypeConverter())
                .WithTypeInspector(x => new SystemTextJsonTypeInspector(x))
                .WithNodeTypeResolver(resolver)
                .WithNodeDeserializer(new KeyValuePairNodeDeserializer())
                .IgnoreUnmatchedProperties()
                .WithAttemptingUnquotedStringTypeDeserialization();

            // Si tu utilises @include : garde ces lignes (sinon, commente-les).
            // var includeOptions = new YamlIncludeNodeDeserializerOptions { DirectoryName = directoryName };
            // var includeNode = new YamlIncludeNodeDeserializer(includeOptions);
            // builder.WithNodeDeserializer(includeNode, o => o.OnTop());
            // builder.WithTagMapping(MdatConstants.IncludeTag, typeof(IncludeRef));

            var deserializer = builder.Build();

            try
            {
                var dict = deserializer.Deserialize<Dictionary<string, object?>>(doc);
                if (dict is null) return Array.Empty<object?>();
                return dict.Values.ToArray();
            }
            catch (YamlException ex)
            {
                // Version sans XunitException
                var pos = Math.Max(1, resolver.Position);
                var pars = runtimeMethod.GetParameters();
                var badParam = (pos - 1) >= 0 && (pos - 1) < pars.Length ? pars[pos - 1].Name : "?";
                throw new AssertFailedException(
                    $"Method '{runtimeMethod.Name}' exception on '{badParam}' parameter.\n{ex.Message}\n{ex.InnerException?.Message}");
            }
        }

        private static string? DescribeTypeOfObject(Type type, string indent, int depth = 0)
        {
            if (depth >= 10) return string.Empty; // garde-fou
            depth++;

            string? obj = null;

            if (type.IsClass && type != typeof(string) && type.FullName is { } fn && !fn.StartsWith("System.", StringComparison.Ordinal))
            {
                foreach (var pi in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var def = GetDefaultValueForProperty(pi);
                    var inner = DescribeTypeOfObject(pi.PropertyType, indent + "  ", depth);
                    obj += $"{indent}{pi.Name}: {(string.IsNullOrEmpty(inner) ? GetDefaultValue(def) : "\n" + inner)}";
                }
            }
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

        private static bool IsGenericEnumerable(Type t, out Type elementType)
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

        private static string GetDefaultValue(object? def)
            => def is { } ? new Serializer().Serialize(def).ReplaceLineEndings("\n") : "null\n";

        private static object? GetDefaultValueForProperty(PropertyInfo property)
        {
            var defaultAttr = property.GetCustomAttribute(typeof(DefaultValueAttribute));
            if (defaultAttr != null)
                return (defaultAttr as DefaultValueAttribute)?.Value;

            var propertyType = property.PropertyType;
            return propertyType.IsValueType ? Activator.CreateInstance(propertyType) : null;
        }

        private static string ToKebabCase(string s)
        {
            var sb = new StringBuilder(s.Length + 8);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (char.IsUpper(c))
                {
                    if (i > 0) sb.Append('-');
                    sb.Append(char.ToLowerInvariant(c));
                }
                else sb.Append(c);
            }
            return sb.ToString();
        }
    }
}