using System.Reflection;
using System.Runtime.CompilerServices;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace MDAT;

/// <summary>
/// TUnit data source reading test cases from a markdown file, e.g. <c>[MarkdownDataSource("~/Tests/{method}.md")]</c>.
/// Rows are produced in-process as factories, so arguments are never serialized by the test framework.
/// The markdown file keeps a link to the C# test that reads it (see <see cref="MarkdownSourceLink"/>).
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class MarkdownDataSourceAttribute : Attribute, IDataSourceAttribute, ITestDiscoveryEventReceiver
{
    private readonly string _filePath;
    private readonly string _sourceFilePath;
    private readonly int _sourceLine;

    /// <param name="filePath">The markdown file, e.g. <c>~/Tests/{method}.md</c>.</param>
    /// <param name="sourceFilePath">Filled by the compiler: the C# file this attribute is written in.</param>
    /// <param name="sourceLine">Filled by the compiler: the line of this attribute.</param>
    public MarkdownDataSourceAttribute(string filePath, [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLine = 0)
    {
        _filePath = filePath;
        _sourceFilePath = sourceFilePath;
        _sourceLine = sourceLine;
    }

    public int Order => 0;

    public bool SkipIfEmpty { get; set; }

    public bool DeferEnumeration { get; set; }

    internal string FilePathTemplate => _filePath;

    internal string DeclaredSourceFilePath => _sourceFilePath;

    internal int DeclaredSourceLine => _sourceLine;

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

        var source = new MarkdownTestSource(_filePath);
        var markdownPath = source.ResolvePath(method);

        var (sourceFilePath, sourceLine) = DeclaredLocation(method);
        var own = new SourceLinkEntry(sourceFilePath, sourceLine, $"{method.DeclaringType?.Name}.{method.Name}");

        // The links of all the tests that read this file are written at once: the lines of the cases are then right for every
        // test of a shared file, none of them is moved afterwards by the link another test adds.
        var entries = SourceLinkPlan.For(method.DeclaringType!.Assembly, Path.GetFullPath(markdownPath), own);

        // Several tests may read the same markdown file while the links are being written: one at a time
        var testCases = MarkdownSourceLink.Synchronized(markdownPath, () =>
        {
            var cases = source.GetCases(method);

            // Links were inserted or corrected in the markdown file: the lines of the cases have moved, so they are read again
            return MarkdownSourceLink.Update(markdownPath, entries)
                ? new MarkdownTestSource(_filePath).GetCases(method)
                : cases;
        });

        foreach (var testCase in testCases)
        {
            yield return TestCaseLocations.ToRow(testCase, dataGeneratorMetadata.TestInformation.Class.Type);
        }
    }

    /// <summary>
    /// Where the attribute is written. TUnit builds its own instance of the attribute in generated code, where the compiler
    /// fills the caller information with the generated file: the one read from the metadata of the method is the right one.
    /// </summary>
    private (string File, int Line) DeclaredLocation(MethodInfo method)
    {
        var declared = method.GetCustomAttributes<MarkdownDataSourceAttribute>(inherit: false)
                             .FirstOrDefault(a => a._filePath == _filePath);

        return declared is null ? (_sourceFilePath, _sourceLine) : (declared._sourceFilePath, declared._sourceLine);
    }
}
