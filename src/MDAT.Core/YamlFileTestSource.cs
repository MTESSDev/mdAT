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
        var fullPath = Path.GetFullPath(path);

        // Each "---" separator is 3 characters long and was removed by the split: this is where a document starts
        var documentStart = 0;

        foreach (var doc in docs)
        {
            var separatorStart = documentStart == 0 ? 0 : documentStart - 3;
            documentStart += doc.Length + 3;

            if (string.IsNullOrWhiteSpace(doc)) continue;

            IDeserializer deserializer = NewDeserializer(testMethod);

            var values = deserializer.Deserialize<Dictionary<string, object>>("---\n" + doc);

            if (values is null) continue;

            var arguments = values.Values.ToArray();

            cases.Add(new MarkdownTestCase(arguments, $"{testMethod.Name}@{_filePath}_#{cases.Count + 1}", fullPath, LineOf(fileData, separatorStart)));
        }

        return cases;
    }

    /// <summary>1-based line of the character at <paramref name="index"/>.</summary>
    private static int LineOf(string text, int index)
    {
        var line = 1;

        for (var i = 0; i < index && i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                line++;
            }
        }

        return line;
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
