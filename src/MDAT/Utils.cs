using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quibble.Xunit;

namespace MDAT;

public static class Extensions
{
    public static void Assert(dynamic? obj, Expected expected)
    {
        if (expected is null) throw new ArgumentNullException(nameof(expected));

        expected!.data ??= "null";

        if (!expected.data.ValidateJSON())
            expected.data = JsonConvert.SerializeObject(expected?.data?.ReplaceLineEndings("\r\n"));

        JsonAssert.EqualOverrideDefault(expected!.data,
                                JsonConvert.SerializeObject(obj),
                                new JsonDiffConfig(expected?.allowAdditionalProperties ?? true));

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
}