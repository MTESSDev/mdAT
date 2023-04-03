
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quibble.Xunit;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MDAT;

public static class Verify
{
    public static Task<T> Assert<T>(Func<Task<T>> functionAMocker, string? expected, [CallerMemberName] string callerName = "")
    {
        return Assert(functionAMocker, new Expected() { data = expected }, callerName);
    }

    public async static Task<T> Assert<T>(Func<Task<T>> functionAMocker, Expected expected, [CallerMemberName] string callerName = "")
    {
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