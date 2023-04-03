using System.Reflection;
using YamlDotNet.Serialization;
using MDAT.Resolver;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Markdig;
using Markdig.Syntax;
using System.ComponentModel;
using YamlDotNet.Core;
using LoxSmoke.DocXml;

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

            _filePath = _filePath.Replace("{method}", testMethod.Name.Replace("_", "-").ToLower());
            ParsedPath = _filePath.Replace('\\', Path.DirectorySeparatorChar).Replace("~" + Path.DirectorySeparatorChar, $"..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}");

            // Get the absolute path to the JSON file
            ParsedPath = Path.IsPathRooted(ParsedPath)
                    ? ParsedPath
                    : Path.GetRelativePath(Directory.GetCurrentDirectory(), ParsedPath);

            if (!File.Exists(ParsedPath))
            {
                CreateMardownFile(testMethod, Assembly.GetCallingAssembly());
                return Array.Empty<object[]>();
            }

            var fileData = File.ReadAllText(ParsedPath);

            var mdFile = Markdown.Parse(fileData);

            string? lastHeading1 = null;
            string? lastHeading2 = null;
            string? lastHeading3 = null;

            List<object[]> to = new List<object[]>();

            foreach (var info in mdFile)
            {
                if (info is HeadingBlock add)
                {
                    if (add?.Level == 1)
                    {
                        lastHeading1 = add?.Inline?.FirstOrDefault()?.ToString();
                        lastHeading2 = null;
                        lastHeading3 = null;
                    }

                    if (add?.Level == 2)
                    {
                        lastHeading2 = add?.Inline?.FirstOrDefault()?.ToString();
                        lastHeading3 = null;
                    }

                    if (add?.Level == 3)
                        lastHeading3 = add?.Inline?.FirstOrDefault()?.ToString();

                }
                else if (info is FencedCodeBlock fbc && fbc.ClosingFencedCharCount == 6 &&
                        (fbc.Info?.ToLower() == "yaml" || fbc.Info?.ToLower() == "yml"))
                {
                    var doc = string.Join("\r\n", fbc.Lines);

                    if (string.IsNullOrWhiteSpace(doc)) continue;

                    var resolver = new MDATYamlTypeResolver(testMethod);

                    IDeserializer deserializer = NewDeserilizer(testMethod, resolver);

                    object[]? values = null;

                    try
                    {
                        var ggs = deserializer.Deserialize<Dictionary<string, object>>(doc);


                        if (ggs is null) continue;
                        values = ggs.Values.ToArray();

                    }
                    catch (YamlException ex)
                    {
                        var invalidName = testMethod.GetParameters()[resolver.Position - 1];
                        throw new InternalTestFailureException($"Method '{testMethod.Name}' exception on '{invalidName.Name}' parameter.'\r\n{ex.Message}\r\n{ex.InnerException?.Message}", ex);
                    }
                    displayNames.Add(values.GetHashCode(), $"{(lastHeading1 is { } ? "_" + lastHeading1 : "")}{(lastHeading2 is { } ? "_" + lastHeading2 : "")}{(lastHeading3 is { } ? "_" + lastHeading3 : "")}");

                    to.Add(values);
                }
            }

            return to;
        }

        private void CreateMardownFile(MethodInfo testMethod, Assembly assembly)
        {
            MethodComments? methodComments = null;

            string directoryPath = GetDirectoryPath(assembly);
            string xmlFilePath = Path.Combine(directoryPath, assembly.GetName().Name + ".xml");
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
                                        ? $"# {paramsDetails.Value.Text}\r\n" 
                                        : "";

                code += $"{strDetails}{item.Name}:\r\n{DescribeTypeOfObject(item.ParameterType, "  ")}";
            }

            var summary = methodComments is { }
                            && !string.IsNullOrWhiteSpace(methodComments.Summary)
                            ? $"\r\n\r\n> {methodComments?.Summary.ReplaceLineEndings("\\\r\n")}"
                            : "";

            File.WriteAllText(ParsedPath, $"# {testMethod.Name}{summary}\r\n\r\n## Case 1\r\n\r\nDescription\r\n\r\n``````yaml\r\n{code}``````");
        }

        public static string GetDirectoryPath(Assembly assembly)
        {
            string codeBase = assembly.Location;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path) ?? throw new InvalidProgramException("Can't get DLL path.");
        }

        static string DescribeTypeOfObject(Type type, string indent)
        {
            string? obj = string.Empty;

            // is a custom class type? describe it too
            if (type.IsClass && !type.FullName!.StartsWith("System."))
            {
                PropertyInfo[] propertyInfos = type.GetProperties();
                foreach (PropertyInfo pi in propertyInfos)
                {
                    var def = GetDefaultValueForProperty(pi);

                    string strDef = def is { } ? new Serializer().Serialize(def).ReplaceLineEndings("\r\n") : "null\r\n";

                    obj += $"{indent}{pi.Name}: {strDef}";

                    // point B, we call the function type this property
                    obj += DescribeTypeOfObject(pi.PropertyType, indent + "  ");
                }
            }

            return obj;
        }

        public static object? GetDefaultValueForProperty(PropertyInfo property)
        {
            var defaultAttr = property.GetCustomAttribute(typeof(DefaultValueAttribute));
            if (defaultAttr != null)
                return (defaultAttr as DefaultValueAttribute)?.Value;

            var propertyType = property.PropertyType;
            return propertyType.IsValueType ? Activator.CreateInstance(propertyType) : null;
        }

        private static IDeserializer NewDeserilizer(MethodInfo testMethod, INodeTypeResolver resolver)
        {
            IDeserializer deserializer = new DeserializerBuilder()
              .WithNodeTypeResolver(resolver)
              //.WithNodeDeserializer(new MDATYamlNodeDeserializer(testMethod))
              .IgnoreUnmatchedProperties()
              .Build();
            return deserializer;
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