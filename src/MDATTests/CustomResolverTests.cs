using MDAT;
using MDATTests.Resolver;
using Microsoft.Extensions.Primitives;

namespace MDATTests;

[TestClass]
public class CustomResolverTests
{
    [ClassInitialize]
    public static void ClassInitialize(TestContext testContext)
    {
        MdatConfig.AddYamlTypeConverter(new StringValuesTestYamlTypeConverter());
    } 

    /// <summary>
    /// Test StringValuesTestYamlTypeConverter
    /// </summary>
    [TestMethod]
    [MarkdownTest("~\\Tests\\{method}.md")]
    public async Task Test_AddYamlTypeConverter(StringValues input, Expected expected)
    {
        _ = await Verify.Assert(() =>
                                    Task.FromResult(input), expected);
    }
}
