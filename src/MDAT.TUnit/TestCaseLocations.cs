using System.Collections.Concurrent;
using TUnit.Core;

namespace MDAT;

/// <summary>
/// Remembers where each test case is written, to point the test explorer to it: double clicking a case then opens its
/// title in the markdown file instead of the C# method shared by all the cases.
/// </summary>
internal static class TestCaseLocations
{
    private static readonly ConcurrentDictionary<string, (string File, int Line)> ByDisplayName = new(StringComparer.Ordinal);

    public static Func<Task<object?[]?>> ToRow(MarkdownTestCase testCase)
    {
        if (!string.IsNullOrEmpty(testCase.FilePath) && testCase.Line > 0)
        {
            ByDisplayName[testCase.DisplayName] = (testCase.FilePath, testCase.Line);
        }

        var row = new TestDataRow<object?[]>(testCase.Arguments, DisplayName: testCase.DisplayName);

        return () => Task.FromResult<object?[]?>(new object?[] { row });
    }

    /// <summary>Gives the discovered test the file and the line of its case, when it comes from a data source of mdAT.</summary>
    public static void Apply(DiscoveredTestContext context)
    {
        if (ByDisplayName.TryGetValue(context.GetDisplayName(), out var location))
        {
            context.TestDetails.TestFilePath = location.File;
            context.TestDetails.TestLineNumber = location.Line;
        }
    }
}
