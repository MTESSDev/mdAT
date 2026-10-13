
using Newtonsoft.Json;
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
            var strData = JsonConvert.SerializeObject(data, Extensions.JsonSerializeSettings());

            Console.WriteLine($"Actual raw for \"{expected?.name ?? TypeTitle(typeof(T))}\":");
            Console.WriteLine(strData);
        }

        return default!;
    }

    /// <summary>
    /// Readable name of a type for the title of the raw output: <c>List&lt;JsonNode&gt;</c> rather than <c>List`1</c>,
    /// <c>Dictionary&lt;String, Byte[]&gt;</c> rather than <c>Dictionary`2</c>.
    /// </summary>
    internal static string TypeTitle(Type type)
    {
        if (type.IsArray)
        {
            return TypeTitle(type.GetElementType()!) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
        }

        var underlying = Nullable.GetUnderlyingType(type);

        if (underlying is not null)
        {
            return TypeTitle(underlying) + "?";
        }

        // the compiler names an anonymous type <>f__AnonymousType0`2
        if (type.Name.StartsWith("<>f__AnonymousType", StringComparison.Ordinal))
        {
            return "anonymous object";
        }

        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var name = type.Name[..type.Name.IndexOf('`')];

        return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(TypeTitle))}>";
    }

}