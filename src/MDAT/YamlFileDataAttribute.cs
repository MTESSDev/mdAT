using MDAT.Resolver;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

namespace MDAT;

[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class YamlFileDataAttribute : Attribute, ITestDataProvider
{
    private readonly string _filePath;
    private readonly Dictionary<int, string> _displayNames = new();

    public YamlFileDataAttribute(string filePath) => _filePath = filePath;

    public IEnumerable<object?[]> GetData(MethodInfo testMethod)
    {
        if (testMethod is null) throw new ArgumentNullException(nameof(testMethod));

        // Résolution de chemin: relatif à l’assembly de test
        var asmDir = Path.GetDirectoryName(testMethod.DeclaringType!.Assembly.Location)!;
        var candidate = _filePath.Replace("~\\", "..\\..\\..\\"); // compat si tu l’utilisais
        var path = Path.IsPathRooted(candidate)
            ? candidate
            : Path.GetFullPath(Path.Combine(asmDir, candidate));

        if (!File.Exists(path))
            throw new FileNotFoundException($"Could not find file at path: {path}");

        var fileData = File.ReadAllText(path);

        // Split multi-docs: --- (en début de ligne)
        var docs = Regex.Split(fileData, @"^---", RegexOptions.Multiline);
        var all = new List<object?[]>();

        foreach (var doc in docs)
        {
            if (string.IsNullOrWhiteSpace(doc)) continue;

            var deser = NewDeserializer(testMethod);
            var dict = deser.Deserialize<Dictionary<string, object?>>("---\n" + doc);
            if (dict is null) continue;

            var arr = dict.Values.ToArray();
            _displayNames[arr.GetHashCode()] = $"{testMethod.Name}@{_filePath}";

            all.Add(arr);
        }

        return all;
    }

    public string? GetDisplayName(MethodInfo methodInfo, object?[]? data)
    {
        if (data is null) return null;
        if (_displayNames.TryGetValue(data.GetHashCode(), out var baseName))
            return $"{baseName}_#{JsonConvert.SerializeObject(data)}";
        return null;
    }

    private static IDeserializer NewDeserializer(MethodInfo testMethod)
        => new DeserializerBuilder()
           .WithNodeTypeResolver(new MDATYamlTypeResolver(testMethod))
           .WithTypeConverter(new ByteArayConverter())
           .WithAttemptingUnquotedStringTypeDeserialization()
           .IgnoreUnmatchedProperties()
           .Build();
}