using MDAT.Resolver;
using System.Reflection;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

namespace MDAT;

/// <summary>
/// Reads test cases from a multi-document YAML file (documents separated by <c>---</c>). Independent of any test framework.
/// </summary>
public sealed class YamlFileTestSource
{
    private readonly string _filePath;

    public YamlFileTestSource(string filePath)
    {
        _filePath = filePath;
    }

    public IReadOnlyList<MarkdownTestCase> GetCases(MethodInfo testMethod)
    {
        if (testMethod == null) { throw new ArgumentNullException(nameof(testMethod)); }

        var path = _filePath.Replace('\\', '/').Replace("~/", "../../../");

        // Get the absolute path to the YAML file
        path = Path.IsPathRooted(path)
                ? path
                : Path.GetRelativePath(Directory.GetCurrentDirectory(), path);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Could not find file at path: {path}", path);
        }

        var fileData = File.ReadAllText(path);

        var docs = Regex.Split(fileData, @"^---", RegexOptions.Multiline);

        var cases = new List<MarkdownTestCase>();

        foreach (var doc in docs)
        {
            if (string.IsNullOrWhiteSpace(doc)) continue;

            IDeserializer deserializer = NewDeserializer(testMethod);

            var values = deserializer.Deserialize<Dictionary<string, object>>("---\n" + doc);

            if (values is null) continue;

            var arguments = values.Values.ToArray();

            cases.Add(new MarkdownTestCase(arguments, $"{testMethod.Name}@{_filePath}_#{cases.Count + 1}"));
        }

        return cases;
    }

    private static IDeserializer NewDeserializer(MethodInfo testMethod)
    {
        return new DeserializerBuilder()
          .WithNodeTypeResolver(new MDATYamlTypeResolver(testMethod))
          .WithTypeConverter(new ByteArayConverter())
          .WithAttemptingUnquotedStringTypeDeserialization()
          .IgnoreUnmatchedProperties()
          .Build();
    }
}
