using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quibble.Xunit;

namespace MDAT;

public static class Extensions
{
    public static void Assert(dynamic? obj, Expected expected)
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

        JsonAssert.EqualOverrideDefault(expected!.data.ToString(),
                                JsonConvert.SerializeObject(obj),
                                new JsonDiffConfig(expected.allowAdditionalProperties));
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