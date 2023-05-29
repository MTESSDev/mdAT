
using Newtonsoft.Json;
using Quibble.Xunit;
using System.Runtime.CompilerServices;

namespace MDAT;

public static class Verify
{
    public static Task<T> Assert<T>(Func<Task<T>> functionAMocker, string? expected, [CallerMemberName] string callerName = "")
    {
        return Assert(functionAMocker, new Expected() { verify = new VerifyStep[] { new VerifyStep { data = expected, type = "match", jsonPath = "$" } } }, callerName);
    }

    public async static Task<T> Assert<T>(Func<Task<T>> functionAMocker, Expected expected, [CallerMemberName] string callerName = "")
    {
        object? data = null;

        try
        {
            var funcReturn = await functionAMocker();
            data = funcReturn;
            await Extensions.Assert(funcReturn, expected);

            return funcReturn;
        }
        catch (Exception ex)
        {
            if (ex is JsonAssertException)
                throw;
            else
            {
                data ??= ex;
                await Extensions.Assert(ex, expected);
            }
        }
        finally
        {
            var strData = JsonConvert.SerializeObject(data, new JsonSerializerSettings
            {
                ContractResolver = new NoResolver(),
                Formatting = Formatting.Indented
            });

            Console.WriteLine("Actual raw:");
            Console.WriteLine(strData);
        }

        return default!;
    }



}