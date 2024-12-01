using LoxSmoke.DocXml;
using Markdig;
using Markdig.Syntax;
using MDAT.Resolver;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel;
using System.Reflection;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.System.Text.Json;

namespace MDAT
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class MarkdownTestAttribute : Attribute, ITestDataSource
    {
        private string _filePath;

        public string ParsedPath { get; set; }

        private Dictionary<int, string> displayNames = new Dictionary<int, string>();

        public MarkdownTestAttribute(string filePath)
        {
            _filePath = filePath;
            ParsedPath = _filePath;
        }

        public IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            if (testMethod == null) { throw new ArgumentNullException(nameof(testMethod)); }

            _filePath = _filePath.Replace("{method}", testMethod.Name.ToKebabCase().ToLower());
            ParsedPath = Extensions.GetCurrentPath(_filePath, false);

            if (!File.Exists(ParsedPath))
            {
                CreateMardownFile(testMethod);
                return Array.Empty<object[]>();
            }

            List<object[]> to = LoadMarkdownFile(testMethod);

            return to;
        }

        private List<object[]> LoadMarkdownFile(MethodInfo testMethod)
        {
            var fileData = File.ReadAllText(ParsedPath);

            var mdFile = Markdown.Parse(fileData);

            string?[] headings = new string?[] { null, null, null, null, null, null };

            List<object[]> to = new();
            List<object[]> toSelected = new();

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

                    displayNames.Add(values.GetHashCode(), "_" + string.Join("_", headings.Where(s => !string.IsNullOrWhiteSpace(s))));

                    if (fbc.Arguments is { } && fbc.Arguments.Contains("selected", StringComparison.InvariantCultureIgnoreCase))
                        toSelected.Add(values);
                    else
                        to.Add(values);
                }
            }

            if (toSelected.Any())
                return toSelected;
            else
                return to;
        }

        private static void SetHeadings(string?[] heading, HeadingBlock add)
        {
            heading[add.Level - 1] = add.Inline?.FirstOrDefault()?.ToString();

            for (int i = add.Level; i < 6; i++)
            {
                heading[i] = null;
            }
        }

        private static object[] ExtractTest(MethodInfo testMethod, string doc, string directoryName)
        {
            var resolver = new MDATYamlTypeResolver(testMethod);

            IDeserializer deserializer = NewDeserializer(resolver, directoryName);

            object[]? values;
            try
            {
                var ggs = deserializer.Deserialize<Dictionary<string, object>>(doc);

                if (ggs is null) return default!;
                values = ggs.Values.ToArray();

            }
            catch (YamlException ex)
            {
                var invalidName = testMethod.GetParameters()[resolver.Position - 1];
                throw new InternalTestFailureException($"Method '{testMethod.Name}' exception on '{invalidName.Name}' parameter.'\n{ex.Message}\n{ex.InnerException?.Message}", ex);
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

            File.WriteAllText(ParsedPath, $"# {testMethod.Name}{summary}\n\n## Case 1\n\nDescription\n\n``````yaml\n{code}``````");
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
              .WithTypeConverter(new SystemTextJsonYamlTypeConverter())
              .WithTypeInspector(x => new SystemTextJsonTypeInspector(x))
              .WithNodeTypeResolver(resolver)
              .WithNodeDeserializer(new KeyValuePairNodeDeserializer())
              .IgnoreUnmatchedProperties()
              .WithAttemptingUnquotedStringTypeDeserialization()
              .WithTagMapping(MdatConstants.IncludeTag, typeof(IncludeRef));

            var includeNodeDeserializerOptions = new YamlIncludeNodeDeserializerOptions
            {
                DirectoryName = directoryName,
                //Builder = deserializer
            };

            var includeNodeDeserializer = new YamlIncludeNodeDeserializer(includeNodeDeserializerOptions);

            deserializer.WithNodeDeserializer(includeNodeDeserializer, s => s.OnTop());

            return deserializer.Build();
        }

        public string? GetDisplayName(MethodInfo methodInfo, object?[]? data)
        {
            var parameters = methodInfo.GetParameters();


            if (data != null)
            {
                var name = displayNames[data.GetHashCode()];
                return $"{methodInfo.Name}@{_filePath}{name}";
            }

            return null;
        }
    }
}