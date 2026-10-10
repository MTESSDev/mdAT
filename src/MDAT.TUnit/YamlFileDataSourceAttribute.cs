using TUnit.Core;
using TUnit.Core.Interfaces;

namespace MDAT;

/// <summary>
/// TUnit data source reading test cases from a multi-document YAML file (documents separated by <c>---</c>).
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class YamlFileDataSourceAttribute : Attribute, IDataSourceAttribute, ITestDiscoveryEventReceiver
{
    private readonly string _filePath;

    public YamlFileDataSourceAttribute(string filePath)
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

        foreach (var testCase in new YamlFileTestSource(_filePath).GetCases(method))
        {
            yield return TestCaseLocations.ToRow(testCase);
        }
    }
}
