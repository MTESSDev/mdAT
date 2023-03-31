
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quibble.Xunit;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MDAT;

public static class Verify
{
    public async static Task<T> Assert<T>(Func<Task<T>> functionAMocker, Expected expected, [CallerMemberName] string callerName = "")
    {
        //expected.data ??= "null";

        try
        {
            var funcReturn = await functionAMocker();
            Extensions.Assert(funcReturn, expected);
            return funcReturn;
        }
        catch (Exception ex)
        {
            if (ex is JsonAssertException)
                throw;
            else
                Extensions.Assert(ex, expected);
        }

        return default!;
    }



}



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
}