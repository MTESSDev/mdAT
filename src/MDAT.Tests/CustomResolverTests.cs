using MDAT;
using MDATTests;
using MDATTests.Resolver;
using Microsoft.Extensions.Primitives;

namespace MDAT.Tests;

public class CustomResolverTests
{
    /// <summary>
    /// Test AddYamlTypeConverter.
    /// <see cref="MdatConfig"/> is global: the converter is registered for the duration of the test only, so the
    /// other tests (which read their cases during discovery, before any test runs) are not affected.
    /// </summary>
    [Test]
    [NotInParallel]
    public async Task Test_AddYamlTypeConverter()
    {
        var converter = new StringValuesTestYamlTypeConverter();
        MdatConfig.AddYamlTypeConverter(converter);

        try
        {
            var method = Utils.GetMethodInfo(() => ConverterTarget);
            var testCase = new MarkdownTestSource("~\\Tests\\test-add-yaml-type-converter.md").GetCases(method).Single();

            _ = await Verify.Assert(() => Task.FromResult((StringValues)testCase.Arguments[0]!), (Expected)testCase.Arguments[1]!);
        }
        finally
        {
            MdatConfig.ListeTypeConverter.RemoveAll(c => ReferenceEquals(c.TypeConverter, converter));
        }
    }

    /// <summary>Signature read by the markdown file of <see cref="Test_AddYamlTypeConverter"/>.</summary>
    public void ConverterTarget(StringValues input, Expected expected) { }
}
