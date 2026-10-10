using System.Reflection;
using TUnit.Core;

namespace MDAT;

/// <summary>
/// TUnit data source reading test cases from a markdown file, e.g. <c>[MarkdownDataSource("~/Tests/{method}.md")]</c>.
/// Rows are produced in-process as factories, so arguments are never serialized by the test framework.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class MarkdownDataSourceAttribute : Attribute, IDataSourceAttribute
{
    private readonly string _filePath;

    public MarkdownDataSourceAttribute(string filePath)
    {
        _filePath = filePath;
    }

    public bool SkipIfEmpty { get; set; }

    public bool DeferEnumeration { get; set; }

    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        await Task.CompletedTask;

        var method = TestMethodResolver.Resolve(dataGeneratorMetadata.TestInformation);

        foreach (var testCase in new MarkdownTestSource(_filePath).GetCases(method))
        {
            yield return TestMethodResolver.ToRow(testCase);
        }
    }
}
