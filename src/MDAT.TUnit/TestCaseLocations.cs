using System.Collections.Concurrent;
using TUnit.Core;

namespace MDAT;

/// <summary>
/// Remembers where each test case is written, to point the test explorer to it: double clicking a case then opens its
/// title in the markdown file instead of the C# method shared by all the cases.
/// </summary>
internal static class TestCaseLocations
{
    // The display name no longer holds the path of the file: the test class keeps two classes apart
    private static readonly ConcurrentDictionary<string, (string File, int Line)> ByDisplayName = new(StringComparer.Ordinal);

    private static string Key(Type? testClass, string displayName) => $"{testClass?.FullName}|{displayName}";

    public static Func<Task<object?[]?>> ToRow(MarkdownTestCase testCase, Type testClass)
    {
        if (!string.IsNullOrEmpty(testCase.FilePath) && testCase.Line > 0)
        {
            ByDisplayName[Key(testClass, testCase.DisplayName)] = (testCase.FilePath, testCase.Line);
        }

        var row = new TestDataRow<object?[]>(testCase.Arguments, DisplayName: testCase.DisplayName);

        return () => Task.FromResult<object?[]?>(new object?[] { row });
    }

    /// <summary>Gives the discovered test the file and the line of its case, when it comes from a data source of mdAT.</summary>
    public static void Apply(DiscoveredTestContext context)
    {
        if (ByDisplayName.TryGetValue(Key(context.TestDetails.ClassType, context.GetDisplayName()), out var location))
        {
            context.TestDetails.TestFilePath = location.File;
            context.TestDetails.TestLineNumber = location.Line;
        }
    }
}
