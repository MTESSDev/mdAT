using MDAT.Resolver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quibble.Xunit;
using YamlDotNet.Serialization;

namespace MDAT;

public static class Extensions
{
    public static async Task Assert(dynamic? obj, Expected expected)
    {
        expected ??= new Expected();

        expected!.data ??= "null";

        if (!expected.data.ToString()!.ValidateJSON())
        {
            if(expected.data is string)
            {
                expected.data = JsonConvert.SerializeObject(expected.data.ToString()!.ReplaceLineEndings("\r\n"));
            }
            else
            {
                expected.data = JsonConvert.SerializeObject(expected.data);
            }
        }

        var data = JsonConvert.SerializeObject(obj);

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

        JsonAssert.EqualOverrideDefault(expected!.data.ToString(),
                                data,
                                new JsonDiffConfig(expected.allowAdditionalProperties));
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
}