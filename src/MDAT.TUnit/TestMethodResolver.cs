using System.Reflection;
using TUnit.Core;

namespace MDAT;

internal static class TestMethodResolver
{
    public static MethodInfo Resolve(MethodMetadata metadata)
    {
        var parameterTypes = metadata.Parameters.Select(p => p.Type).ToArray();

        return metadata.Class.Type.GetMethod(
                   metadata.Name,
                   BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
                   binder: null,
                   parameterTypes,
                   modifiers: null)
               ?? throw new InvalidOperationException($"Could not resolve test method '{metadata.Class.Type.FullName}.{metadata.Name}'.");
    }

    public static Func<Task<object?[]?>> ToRow(MarkdownTestCase testCase)
    {
        var row = new TestDataRow<object?[]>(testCase.Arguments, DisplayName: testCase.DisplayName);

        return () => Task.FromResult<object?[]?>(new object?[] { row });
    }
}
