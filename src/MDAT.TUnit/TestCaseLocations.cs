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

    // The properties (priority=High) cannot go on the data row like the categories: they are given when the test is discovered
    private static readonly ConcurrentDictionary<string, IReadOnlyList<KeyValuePair<string, string>>> PropertiesByDisplayName = new(StringComparer.Ordinal);

    private static string Key(Type? testClass, string displayName) => $"{testClass?.FullName}|{displayName}";

    public static Func<Task<object?[]?>> ToRow(MarkdownTestCase testCase, Type testClass)
    {
        if (!string.IsNullOrEmpty(testCase.FilePath) && testCase.Line > 0)
        {
            ByDisplayName[Key(testClass, testCase.DisplayName)] = (testCase.FilePath, testCase.Line);
        }

        if (testCase.Properties.Count > 0)
        {
            // Two cases with the same name cannot be told apart here: when their properties differ, none gets any, so that
            // a case never receives the properties of another one.
            PropertiesByDisplayName.AddOrUpdate(
                Key(testClass, testCase.DisplayName),
                _ => testCase.Properties,
                (_, known) => known.SequenceEqual(testCase.Properties) ? known : Array.Empty<KeyValuePair<string, string>>());
        }

        // a case marked "skipped" in the markdown file is reported as ignored by the test explorer and never run;
        // its categories (category=Smoke,Fast, or tags: category: in the front matter) are the ones of the test
        var row = new TestDataRow<object?[]>(
            testCase.Arguments,
            DisplayName: testCase.DisplayName,
            Skip: testCase.SkipReason,
            Categories: testCase.Categories.Count > 0 ? testCase.Categories.ToArray() : null);

        return () => Task.FromResult<object?[]?>(new object?[] { row });
    }

    /// <summary>Gives the discovered test the file and the line of its case, and its properties, when it comes from a data source of mdAT.</summary>
    public static void Apply(DiscoveredTestContext context)
    {
        var key = Key(context.TestDetails.ClassType, context.GetDisplayName());

        if (ByDisplayName.TryGetValue(key, out var location))
        {
            context.TestDetails.TestFilePath = location.File;
            context.TestDetails.TestLineNumber = location.Line;
        }

        if (PropertiesByDisplayName.TryGetValue(key, out var properties))
        {
            foreach (var property in properties)
            {
                context.AddProperty(property.Key, property.Value);
            }
        }
    }
}
