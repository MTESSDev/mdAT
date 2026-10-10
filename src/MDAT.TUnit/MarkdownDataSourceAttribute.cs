using System.Reflection;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace MDAT;

/// <summary>
/// TUnit data source reading test cases from a markdown file, e.g. <c>[MarkdownDataSource("~/Tests/{method}.md")]</c>.
/// Rows are produced in-process as factories, so arguments are never serialized by the test framework.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class MarkdownDataSourceAttribute : Attribute, IDataSourceAttribute, ITestDiscoveryEventReceiver
{
    private readonly string _filePath;

    public MarkdownDataSourceAttribute(string filePath)
    {
        _filePath = filePath;
    }

    public int Order => 0;

    public bool SkipIfEmpty { get; set; }

    public bool DeferEnumeration { get; set; }

    /// <summary>Points the test explorer to the case in its file (see <see cref="MarkdownTestCase.Line"/>).</summary>
    public ValueTask OnTestDiscovered(DiscoveredTestContext context)
    {
        TestCaseLocations.Apply(context);
        return default;
    }

    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        await Task.CompletedTask;

        var method = TestMethodResolver.Resolve(dataGeneratorMetadata.TestInformation);

        foreach (var testCase in new MarkdownTestSource(_filePath).GetCases(method))
        {
            yield return TestCaseLocations.ToRow(testCase);
        }
    }
}
