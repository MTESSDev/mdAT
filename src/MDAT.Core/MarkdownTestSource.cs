using LoxSmoke.DocXml;
using Markdig;
using Markdig.Syntax;
using MDAT.Resolver;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace MDAT
{
    /// <summary>
    /// One test case extracted from a markdown file: the arguments to give to the test method and its display name.
    /// </summary>
    public sealed class MarkdownTestCase
    {
        public MarkdownTestCase(object?[] arguments, string displayName)
        {
            Arguments = arguments;
            DisplayName = displayName;
        }

        public object?[] Arguments { get; }

        public string DisplayName { get; }
    }

    /// <summary>
    /// Reads test cases from a markdown file. Independent of any test framework.
    /// </summary>
    public sealed class MarkdownTestSource
    {
        private string _filePath;

        public string ParsedPath { get; private set; }

        public MarkdownTestSource(string filePath)
        {
            _filePath = filePath;
            ParsedPath = _filePath;
        }

        /// <summary>
        /// Loads the test cases for <paramref name="testMethod"/>. When the markdown file does not exist, it is
        /// generated from the method signature and no case is returned.
        /// </summary>
        public IReadOnlyList<MarkdownTestCase> GetCases(MethodInfo testMethod)
        {
            if (testMethod == null) { throw new ArgumentNullException(nameof(testMethod)); }

            _filePath = _filePath.Replace("{method}", testMethod.Name.ToKebabCase().ToLower());
            ParsedPath = Extensions.GetCurrentPath(_filePath, false);

            if (!File.Exists(ParsedPath))
            {
                CreateMardownFile(testMethod);
                return Array.Empty<MarkdownTestCase>();
            }

            var to = LoadMarkdownFile(testMethod);

            return to;
        }

        private List<MarkdownTestCase> LoadMarkdownFile(MethodInfo testMethod)
        {
            var fileData = File.ReadAllText(ParsedPath);

            var mdFile = Markdown.Parse(fileData);

            string?[] headings = new string?[] { null, null, null, null, null, null };

            List<MarkdownTestCase> to = new();
            List<MarkdownTestCase> toSelected = new();

            foreach (var info in mdFile)
            {
                if (info is HeadingBlock add)
                {
                    SetHeadings(headings, add);
                }
                else if (info is FencedCodeBlock fbc
                        && fbc.ClosingFencedCharCount == 6
                        && (fbc.Info?.ToLower() == "yaml" || fbc.Info?.ToLower() == "yml"))
                {
                    var doc = string.Join("\n", fbc.Lines);

                    if (string.IsNullOrWhiteSpace(doc))
                        continue;

                    if (fbc.Arguments is { } && fbc.Arguments.Contains("skipped", StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    var values = ExtractTest(testMethod, doc, Path.GetDirectoryName(ParsedPath)!);

                    if (values is null) continue;

                    var testCase = new MarkdownTestCase(values, DisplayName(testMethod, headings));

                    if (fbc.Arguments is { } && fbc.Arguments.Contains("selected", StringComparison.InvariantCultureIgnoreCase))
                        toSelected.Add(testCase);
                    else
                        to.Add(testCase);
                }
            }

            if (toSelected.Any())
                return toSelected;
            else
                return to;
        }

        private string DisplayName(MethodInfo testMethod, string?[] headings)
        {
            return $"{testMethod.Name}@{_filePath}_{string.Join("_", headings.Where(h => !string.IsNullOrWhiteSpace(h)))}";
        }

        private static void SetHeadings(string?[] heading, HeadingBlock add)
        {
            heading[add.Level - 1] = add.Inline?.FirstOrDefault()?.ToString();

            for (int i = add.Level; i < 6; i++)
            {
                heading[i] = null;
            }
        }

        private static object[]? ExtractTest(MethodInfo testMethod, string doc, string directoryName)
        {
            var resolver = new MDATYamlTypeResolver(testMethod);

            IDeserializer deserializer = NewDeserializer(resolver, directoryName);

            object[]? values;
            try
            {
                var ggs = deserializer.Deserialize<Dictionary<string, object>>(doc);

                if (ggs is null) return null;
                values = ggs.Values.ToArray();

            }
            catch (YamlException ex)
            {
                var invalidName = testMethod.GetParameters()[resolver.Position - 1];
                throw new MdatTestDefinitionException($"Method '{testMethod.Name}' exception on '{invalidName.Name}' parameter.'\n{ex.Message}\n{ex.InnerException?.Message}", ex);
            }

            return values;
        }

        private void CreateMardownFile(MethodInfo testMethod)
        {
            MethodComments? methodComments = null;

            string directoryPath = testMethod.DeclaringType!.Assembly.Location;
            string xmlFilePath = directoryPath.Replace(".dll", ".xml");

            if (File.Exists(xmlFilePath))
            {
                DocXmlReader reader = new DocXmlReader(xmlFilePath);
                methodComments = reader.GetMethodComments(testMethod);
            }

            var code = "";

            foreach (var item in testMethod.GetParameters())
            {
                var paramsDetails = methodComments?.Parameters.Where(e => e.Name == item.Name).FirstOrDefault();

                var strDetails = paramsDetails is { }
                                        && paramsDetails.Value.Text is { }
                                        ? $"# {paramsDetails.Value.Text}\n"
                                        : "";

                code += $"{strDetails}{item.Name}:\n{DescribeTypeOfObject(item.ParameterType, "  ")}";
            }

            var summary = methodComments is { }
                            && !string.IsNullOrWhiteSpace(methodComments.Summary)
                            ? $"\n\n> {methodComments?.Summary.ReplaceLineEndings("\\\n")}"
                            : "";

            File.WriteAllText(ParsedPath, $"# {testMethod.Name}{summary}\n\n## Case 1\n\nDescription\n\n``````yaml\n{code}``````", Encoding.UTF8);
        }

        static string? DescribeTypeOfObject(Type type, string indent, int pos = 0)
        {
            if (pos == 10)
                return string.Empty; // Loop protection
            else
                pos++;

            string? obj = null;

            if (type.IsClass && !type.FullName!.StartsWith("System."))
            {
                PropertyInfo[] propertyInfos = type.GetProperties();
                foreach (PropertyInfo pi in propertyInfos)
                {
                    var def = GetDefaultValueForProperty(pi);

                    var innerObj = DescribeTypeOfObject(pi.PropertyType, indent + "  ", pos);

                    obj += $"{indent}{pi.Name}: {(string.IsNullOrEmpty(innerObj) ? GetDefaultValue(def) : "\n" + innerObj)}";
                }
            }
            else if (type.Name.StartsWith("IEnumerable"))
            {
                PropertyInfo[] propertyInfos = type.GetGenericArguments()[0].GetProperties();

                var premier = true;

                foreach (PropertyInfo pi in propertyInfos)
                {
                    var def = GetDefaultValueForProperty(pi);

                    var innerObj = DescribeTypeOfObject(pi.PropertyType, indent + "    ", pos);

                    obj += $"{indent}{(premier ? "- " : "  ")}{pi.Name}: {(string.IsNullOrEmpty(innerObj) ? GetDefaultValue(def) : "\n" + innerObj)}";
                    premier = false;
                }
            }


            return obj;
        }

        private static string GetDefaultValue(object? def)
        {
            return def is { } ? new Serializer().Serialize(def).ReplaceLineEndings("\n") : "null\n";
        }

        public static object? GetDefaultValueForProperty(PropertyInfo property)
        {
            var defaultAttr = property.GetCustomAttribute(typeof(DefaultValueAttribute));
            if (defaultAttr != null)
                return (defaultAttr as DefaultValueAttribute)?.Value;

            var propertyType = property.PropertyType;
            return propertyType.IsValueType ? Activator.CreateInstance(propertyType) : null;
        }

        private static IDeserializer NewDeserializer(INodeTypeResolver resolver, string directoryName)
        {
            DeserializerBuilder deserializer = new DeserializerBuilder()
              .WithTypeConverter(new ByteArayConverter(), e => e.OnBottom())
              .WithTypeConverter(new SystemTextJsonYamlTypeConverter(false, true))
              .WithTypeInspector(x => new YamlDotNet.System.Text.Json.SystemTextJsonTypeInspector(x))
              .WithTypeConverter(new StringValuesConverter())
              .WithNodeTypeResolver(resolver)
              .WithNodeDeserializer(new KeyValuePairNodeDeserializer())
              .IgnoreUnmatchedProperties()
              .WithAttemptingUnquotedStringTypeDeserialization()
              .WithTagMapping(MdatConstants.IncludeTag, typeof(IncludeRef));

            foreach (var typeConverter in MdatConfig.ListeTypeConverter)
            {
                _ = typeConverter.Where is { } ? deserializer.WithTypeConverter(typeConverter.TypeConverter!, typeConverter.Where) : deserializer.WithTypeConverter(typeConverter.TypeConverter!);
            }

            var includeNodeDeserializerOptions = new YamlIncludeNodeDeserializerOptions
            {
                DirectoryName = directoryName,
                //Builder = deserializer
            };

            var includeNodeDeserializer = new YamlIncludeNodeDeserializer(includeNodeDeserializerOptions);

            deserializer.WithNodeDeserializer(includeNodeDeserializer, s => s.OnTop());

            return deserializer.Build();
        }
    }
}