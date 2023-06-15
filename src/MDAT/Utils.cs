using MDAT.Resolver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Quibble.Xunit;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

namespace MDAT;

public class NoResolver : DefaultContractResolver
{
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        JsonProperty property = base.CreateProperty(member, memberSerialization);
        property.PropertyName = member.Name;
        return property;
    }
}

public static class Extensions
{
    public static async Task Assert(dynamic? obj, Expected expected)
    {
        expected ??= new Expected();

        if (expected!.verify is null)
            expected!.verify = new VerifyStep[] { new VerifyStep { data = "null", jsonPath = "$", type = "match" } };

        foreach (var item in expected!.verify)
        {
            if (item.type == "match")
            {
                await MatchCheck(obj, expected, item);
            }
            else
            {
                throw new InvalidOperationException($"Invalid verify type '{item.type}'.");
            }
        }
    }

    private static async Task MatchCheck(dynamic obj, Expected expected, VerifyStep item)
    {
        object? finalExpectedData = item.data;

        finalExpectedData = DataAdapter(item.data, finalExpectedData);

        var data = JsonConvert.SerializeObject(obj, new JsonSerializerSettings
        {
            ContractResolver = new NoResolver(),
            Formatting = Formatting.Indented
        });

        if (!string.IsNullOrWhiteSpace(expected.generateExpectedData))
        {
            var path = GetCurrentPath(expected.generateExpectedData, true);

            if (path.EndsWith(".yml", StringComparison.InvariantCultureIgnoreCase))
            {
                var builder = new SerializerBuilder()
                    .WithTypeConverter(new ByteArayConverter())
                    .Build();
                var yml = builder.Serialize(obj);

                await File.WriteAllTextAsync(path, yml);
            }
            else
            {
                await File.WriteAllTextAsync(path, data);
            }
        }

        JToken linq = JToken.Parse(data);

        Regex regex = new Regex(@"^(?<jsonPath>.+?)(?<last>\.length\(\))?\r?$");

        Match match = regex.Match(item.jsonPath);

        var tokens = linq.SelectTokens(match.Groups[1].Value);

        string? finalData;

        if (match.Groups[2].Success)
        {
            switch (match.Groups[2].Value)
            {
                case ".length()":
                    if (tokens.Count() == 1)
                        if (tokens.FirstOrDefault()!.Type == JTokenType.String)
                            finalData = tokens.ToString()!.Length.ToString();
                        else
                            finalData = tokens.Children().Count().ToString();
                    else
                        finalData = tokens.Count().ToString();
                    break;
                default:
                    throw new InvalidOperationException($"Invalid verb '{match.Groups[2].Value}'");
            }
        }
        else
        {
            if (tokens.Count() == 1)
                finalData = HandleValue(tokens?.FirstOrDefault());
            else
                finalData = HandleValue(JToken.FromObject(tokens));

            finalData = DataAdapter(finalData, finalData)?.ToString();
        }

        JsonAssert.EqualOverrideDefault(finalExpectedData?.ToString(),
                                finalData,
                                new JsonDiffConfig(item.allowAdditionalProperties));
    }

    private static string? HandleValue(JToken? jToken)
    {
        if (jToken is null) return null;

        if (jToken.Type == JTokenType.Null) return null;

        return jToken.ToString();
    }

    private static object? DataAdapter(object? data, object? finalData)
    {
        if (data is null || !data.ToString()!.ValidateJSON())
        {
            if (data is string)
            {
                finalData = JsonConvert.SerializeObject(data.ToString()!.ReplaceLineEndings("\n"));
            }
            else
            {
                finalData = JsonConvert.SerializeObject(data, new JsonSerializerSettings
                {
                    ContractResolver = new NoResolver(),
                    Formatting = Formatting.Indented
                });
            }
        }

        return finalData;
    }

    public static string GetCurrentPath(string filePath, bool forceLocal)
    {
        if (forceLocal)
        {
            filePath = "~" + Path.DirectorySeparatorChar + AbsolutePath(filePath);
        }

        var path = filePath.ReplacePlatformCompatiblePath().Replace("~" + Path.DirectorySeparatorChar, $"..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}");

        path = AbsolutePath(path);

        return path;
    }

    private static string AbsolutePath(string path)
    {
        // Get the absolute path to the JSON file
        path = Path.IsPathRooted(path)
                        ? path
                        : Path.GetRelativePath(Directory.GetCurrentDirectory(), path);
        return path;
    }

    public static bool ValidateJSON(this string s)
    {
        try
        {
            JToken.Parse(s);
            return true;
        }
        catch (JsonReaderException)
        {
            return false;
        }
    }

    public static string ReplacePlatformCompatiblePath(this string path)
    {
        return path.Replace('\\', Path.DirectorySeparatorChar)
                   .Replace('/', Path.DirectorySeparatorChar);
    }

    public static Dictionary<object, object> FixDict(this Dictionary<object, object> dictionary)
    {
        Dictionary<object, object> dict = new();

        foreach (var item in dictionary)
        {
            if (item.Value is object[] dic)
                dict.Add(item.Key, FixDict(dic));
            else
                dict.Add(item.Key, item.Value);
        }

        return dict;
    }

    private static Dictionary<object, object> FixDict(object[] obj)
    {
        Dictionary<object, object> dict = new();

        foreach (var item in obj)
        {
            if (item is KeyValuePair<object, object> keypair)
                if (keypair.Value is object[])
                    dict.Add(keypair.Key, FixDict((object[])keypair.Value));
                else
                    dict.Add(keypair.Key, keypair.Value);
        }

        return dict;
    }

    public static string ToKebabCase(this string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException(nameof(text));
        }
        if (text.Length < 2)
        {
            return text;
        }

        text = text.Replace("_", "-");

        var sb = new StringBuilder();
        sb.Append(char.ToLowerInvariant(text[0]));
        for (int i = 1; i < text.Length; ++i)
        {
            char c = text[i];
            if (char.IsUpper(c))
            {
                sb.Append('-');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString().Replace("--", "-");
    }
}