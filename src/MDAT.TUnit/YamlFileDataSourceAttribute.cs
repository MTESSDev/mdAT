using TUnit.Core;

namespace MDAT;

/// <summary>
/// TUnit data source reading test cases from a multi-document YAML file (documents separated by <c>---</c>).
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class YamlFileDataSourceAttribute : Attribute, IDataSourceAttribute
{
    private readonly string _filePath;

    public YamlFileDataSourceAttribute(string filePath)
    {
        _filePath = filePath;
    }

    public bool SkipIfEmpty { get; set; }

    public bool DeferEnumeration { get; set; }

    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        await Task.CompletedTask;

        var method = TestMethodResolver.Resolve(dataGeneratorMetadata.TestInformation);

        foreach (var testCase in new YamlFileTestSource(_filePath).GetCases(method))
        {
            yield return TestMethodResolver.ToRow(testCase);
        }
    }
}
