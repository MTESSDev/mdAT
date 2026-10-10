using MDAT;
using MDATTests.Resolver;
using Microsoft.Extensions.Primitives;

[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]

namespace MDAT.Tests;

[TestClass]
public class CustomResolverTests
{
    [ClassInitialize]
    public static void ClassInitialize(TestContext testContext)
    {
        MdatConfig.AddYamlTypeConverter(new StringValuesTestYamlTypeConverter());
    }

    /// <summary>
    /// Test AddYamlTypeConverter
    /// </summary>
    [TestMethod]
    [DoNotParallelize]
    [MarkdownTest("~\\Tests\\{method}.md")]
    public async Task Test_AddYamlTypeConverter(StringValues input, Expected expected)
    {
        _ = await Verify.Assert(() =>
                                    Task.FromResult(input), expected);
    }
}
